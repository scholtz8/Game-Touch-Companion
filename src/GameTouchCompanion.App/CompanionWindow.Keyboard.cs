using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GameTouchCompanion.Core;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace GameTouchCompanion.App;

public partial class CompanionWindow
{
    private enum TouchKeyboardTarget
    {
        None,
        Address,
        WebPage
    }

    private TouchKeyboardTarget keyboardTarget;
    private TabRuntime? keyboardTab;
    private bool keyboardShift;
    private bool keyboardLarge;
    private bool replaceAddressOnNextInput;
    private bool addressEditing;

    private async Task InstallKeyboardBridgeAsync(TabRuntime tab)
    {
        if (tab.Core is null) return;
        var token = JsonSerializer.Serialize(tab.MessageToken);
        var script = KeyboardBridgeScript.Replace("__GTC_TOKEN__", token, StringComparison.Ordinal);
        await tab.Core.AddScriptToExecuteOnDocumentCreatedAsync(script);
    }

    private const string KeyboardBridgeScript = """
(() => {
  const token = __GTC_TOKEN__;
  const post = (visible) => {
    try { chrome.webview.postMessage({ type: visible ? 'gtc-keyboard-show' : 'gtc-keyboard-hide', token }); } catch (_) {}
  };
  const editable = (el) => {
    if (!el || el.disabled || el.readOnly) return false;
    if (el.isContentEditable) return true;
    if (el.tagName === 'TEXTAREA') return true;
    if (el.tagName !== 'INPUT') return false;
    const t = (el.type || 'text').toLowerCase();
    return ['text','search','url','tel','email','password','number'].includes(t);
  };
  document.addEventListener('pointerup', e => { if (e.pointerType === 'touch') post(editable(e.target)); }, true);
  window.__gtcKeyboard = {
    insert(text) {
      const el = document.activeElement;
      if (!editable(el)) return false;
      if (el.isContentEditable) {
        document.execCommand('insertText', false, text);
      } else {
        const value = el.value ?? '';
        const start = typeof el.selectionStart === 'number' ? el.selectionStart : value.length;
        const end = typeof el.selectionEnd === 'number' ? el.selectionEnd : value.length;
        if (typeof el.setRangeText === 'function') el.setRangeText(text, start, end, 'end');
        else el.value = value.slice(0, start) + text + value.slice(end);
        el.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: text }));
      }
      return true;
    },
    backspace() {
      const el = document.activeElement;
      if (!editable(el)) return false;
      if (el.isContentEditable) {
        document.execCommand('delete', false, null);
      } else {
        const value = el.value ?? '';
        let start = typeof el.selectionStart === 'number' ? el.selectionStart : value.length;
        let end = typeof el.selectionEnd === 'number' ? el.selectionEnd : value.length;
        if (start === end && start > 0) start--;
        if (typeof el.setRangeText === 'function') el.setRangeText('', start, end, 'end');
        else el.value = value.slice(0, start) + value.slice(end);
        el.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'deleteContentBackward', data: null }));
      }
      return true;
    },
    clear() {
      const el = document.activeElement;
      if (!editable(el)) return false;
      if (el.isContentEditable) el.textContent = '';
      else el.value = '';
      el.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'deleteContentBackward', data: null }));
      return true;
    },
    enter() {
      const el = document.activeElement;
      if (!editable(el)) return false;
      if (el.tagName === 'TEXTAREA' || el.isContentEditable) return this.insert('\n');
      const down = new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', bubbles: true, cancelable: true });
      const allowed = el.dispatchEvent(down);
      el.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', code: 'Enter', bubbles: true }));
      if (allowed && el.form && typeof el.form.requestSubmit === 'function') el.form.requestSubmit();
      return true;
    }
  };
})();
""";

    private void WebMessageReceived(TabRuntime tab, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (isClosing || tab.Core is null || !BrowserUrlPolicy.IsAllowed(e.Source)) return;
        try
        {
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("token", out var token) || token.ValueKind != JsonValueKind.String ||
                !string.Equals(token.GetString(), tab.MessageToken, StringComparison.Ordinal) ||
                !root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String) return;

            switch (type.GetString())
            {
                case "gtc-keyboard-show":
                    if (ReferenceEquals(tab, activeTab)) ShowTouchKeyboard(TouchKeyboardTarget.WebPage, tab);
                    break;
                case "gtc-keyboard-hide":
                    if (ReferenceEquals(tab, activeTab) && keyboardTarget == TouchKeyboardTarget.WebPage) HideTouchKeyboard();
                    break;
            }
        }
        catch (JsonException ex)
        {
            Log.Debug(ex, "Ignored invalid WebView keyboard bridge message. Tab={Tab}", tab.Definition.Id);
        }
    }

    private void AddressBox_PreviewMouseDown(object sender, MouseButtonEventArgs e) => BeginAddressEdit();
    private void AddressBox_PreviewTouchDown(object sender, TouchEventArgs e) => BeginAddressEdit();

    private void BeginAddressEdit()
    {
        addressEditing = true;
        replaceAddressOnNextInput = true;
        AddressBox.SelectAll();
        ShowTouchKeyboard(TouchKeyboardTarget.Address, activeTab);
    }

    private void NavigateAddress_Click(object sender, RoutedEventArgs e) => NavigateAddressBar();

    private void NavigateAddressBar()
    {
        if (activeTab is null) return;
        var input = AddressBox.Text.Trim();
        if (!BrowserUrlPolicy.TryNormalize(input, out var normalized) &&
            !BrowserUrlPolicy.TryNormalize("https://" + input, out normalized))
        {
            viewModel.ReportError("Introduce una dirección HTTP o HTTPS válida, sin credenciales. Otros esquemas no están permitidos.");
            return;
        }
        addressEditing = false;
        replaceAddressOnNextInput = false;
        HideTouchKeyboard();
        Navigate(activeTab, normalized);
    }

    private void SyncAddressBar(string url)
    {
        if (addressEditing && keyboardTarget == TouchKeyboardTarget.Address) return;
        AddressBox.Text = url;
        AddressBox.SelectionStart = AddressBox.Text.Length;
        AddressBox.SelectionLength = 0;
    }

    private void ShowTouchKeyboard(TouchKeyboardTarget target, TabRuntime? tab)
    {
        keyboardTarget = target;
        keyboardTab = tab;
        TouchKeyboardPanel.Visibility = Visibility.Visible;
    }

    private void HideTouchKeyboard()
    {
        TouchKeyboardPanel.Visibility = Visibility.Collapsed;
        keyboardTarget = TouchKeyboardTarget.None;
        keyboardTab = null;
        keyboardShift = false;
        KeyboardShiftAction.Content = "⇧";
        if (addressEditing)
        {
            addressEditing = false;
            replaceAddressOnNextInput = false;
            if (activeTab is not null) SyncAddressBar(activeTab.CurrentUrl);
        }
    }

    private void HideKeyboard_Click(object sender, RoutedEventArgs e) => HideTouchKeyboard();

    private void ToggleKeyboardSize_Click(object sender, RoutedEventArgs e)
    {
        keyboardLarge = !keyboardLarge;
        var scale = keyboardLarge ? 1.25 : 1.0;
        KeyboardScaleTransform.ScaleX = scale;
        KeyboardScaleTransform.ScaleY = scale;
        KeyboardSizeAction.Content = keyboardLarge ? "A−" : "A+";
        Log.Debug("Touch keyboard size changed. Large={Large}; Scale={Scale}", keyboardLarge, scale);
    }

    private async void KeyboardKey_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string value) return;
        if (keyboardShift && value.Length == 1 && char.IsLetter(value[0])) value = value.ToUpperInvariant();
        await SendKeyboardTextAsync(value);
        if (keyboardShift)
        {
            keyboardShift = false;
            KeyboardShiftAction.Content = "⇧";
        }
    }

    private async void KeyboardSpecialKey_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string action) return;
        switch (action)
        {
            case "shift":
                keyboardShift = !keyboardShift;
                KeyboardShiftAction.Content = keyboardShift ? "⇧●" : "⇧";
                return;
            case "space":
                await SendKeyboardTextAsync(" ");
                return;
            case "backspace":
                await SendKeyboardActionAsync("backspace");
                return;
            case "clear":
                await SendKeyboardActionAsync("clear");
                return;
            case "enter":
                if (keyboardTarget == TouchKeyboardTarget.Address) NavigateAddressBar();
                else await SendKeyboardActionAsync("enter");
                return;
        }
    }

    private async Task SendKeyboardTextAsync(string text)
    {
        if (keyboardTarget == TouchKeyboardTarget.Address)
        {
            ReplaceAddressSelection(text);
            return;
        }
        if (keyboardTarget != TouchKeyboardTarget.WebPage || keyboardTab?.Core is null) return;
        var json = JsonSerializer.Serialize(text);
        try { await keyboardTab.Core.ExecuteScriptAsync($"window.__gtcKeyboard?.insert({json})"); }
        catch (Exception ex) { LogBrowserFailure("touch-keyboard-insert", ex); }
    }

    private async Task SendKeyboardActionAsync(string action)
    {
        if (keyboardTarget == TouchKeyboardTarget.Address)
        {
            switch (action)
            {
                case "backspace": BackspaceAddress(); break;
                case "clear": ClearAddress(); break;
            }
            return;
        }
        if (keyboardTarget != TouchKeyboardTarget.WebPage || keyboardTab?.Core is null) return;
        if (action is not ("backspace" or "clear" or "enter")) return;
        try { await keyboardTab.Core.ExecuteScriptAsync($"window.__gtcKeyboard?.{action}()"); }
        catch (Exception ex) { LogBrowserFailure("touch-keyboard-" + action, ex); }
    }

    private void ReplaceAddressSelection(string text)
    {
        if (replaceAddressOnNextInput)
        {
            AddressBox.Text = text;
            replaceAddressOnNextInput = false;
            AddressBox.SelectionStart = AddressBox.Text.Length;
            AddressBox.SelectionLength = 0;
            return;
        }
        var start = Math.Clamp(AddressBox.SelectionStart, 0, AddressBox.Text.Length);
        var length = Math.Clamp(AddressBox.SelectionLength, 0, AddressBox.Text.Length - start);
        AddressBox.Text = AddressBox.Text.Remove(start, length).Insert(start, text);
        AddressBox.SelectionStart = start + text.Length;
        AddressBox.SelectionLength = 0;
    }

    private void BackspaceAddress()
    {
        if (replaceAddressOnNextInput)
        {
            ClearAddress();
            return;
        }
        var start = Math.Clamp(AddressBox.SelectionStart, 0, AddressBox.Text.Length);
        var length = Math.Clamp(AddressBox.SelectionLength, 0, AddressBox.Text.Length - start);
        if (length > 0)
        {
            AddressBox.Text = AddressBox.Text.Remove(start, length);
            AddressBox.SelectionStart = start;
        }
        else if (start > 0)
        {
            AddressBox.Text = AddressBox.Text.Remove(start - 1, 1);
            AddressBox.SelectionStart = start - 1;
        }
        AddressBox.SelectionLength = 0;
    }

    private void ClearAddress()
    {
        AddressBox.Text = string.Empty;
        AddressBox.SelectionStart = 0;
        AddressBox.SelectionLength = 0;
        replaceAddressOnNextInput = false;
    }
}
