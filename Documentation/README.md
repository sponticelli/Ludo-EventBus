# Ludo EventBus

A lightweight, type-safe event system for Unity games with priority-based event handling and diagnostics.

## Overview

The Ludo EventBus is a powerful, flexible event system designed for Unity games. It provides a clean way to decouple game systems through a publish-subscribe pattern, allowing components to communicate without direct references to each other.

Key features:
- Type-safe event handling
- Priority-based event processing
- Multiple subscription methods (attribute-based and dynamic)
- Built-in diagnostics and performance monitoring
- Event cancellation support
- Editor tools for debugging and analysis

## Documentation

- [Architectur](./Architecture.md) - Internal architecture and design of the EventBus system.
- [Usage Guide](./Usage-Guide.md) - How to use the EventBus system
- [API Documentation](./API-Documentation.md) - Core interfaces and classes
- [Patterns & Anti-patterns](./Patterns-Antipatterns.md) - Recommended patterns and anti-patterns
- [Diagnostics Guide](./Diagnostics-Guide.md) - How to use the diagnostics tools
- [Examples](./Examples.md) - Few examples

## Installation

The EventBus is packaged as a UPM package. You can add it to your project via the Package Manager or by adding the following to your `manifest.json`:

```json
"dependencies": {
  "com.ludo.core.eventbus": "1.0.0"
}
```