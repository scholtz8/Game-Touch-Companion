# ADR-0001: Separate configuration and companion windows

## Context

Configuration needs keyboard/accessibility while touch companion must not activate.

## Problem

Changing one WPF window repeatedly between normal and no-activate modes adds focus-state complexity.

## Options

One mutable window; two purpose-specific windows.

## Decision

Use activatable `MainWindow` and independent no-activate `CompanionWindow`.

## Consequences

Focus behavior is easier to reason about; configuration remains accessible; shared state will be needed in later phases.
