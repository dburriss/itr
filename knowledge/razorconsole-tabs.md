# Tabs in a TUI with RazorConsole / Spectre.Console

Neither Spectre.Console nor RazorConsole ships a built-in `<Tabs>` component. With RazorConsole you have all the primitives needed to build one yourself.

## Option A: `TextButton` row as a tab bar (recommended)

A horizontal row of `TextButton` components acting as tab headers, with conditional rendering for the body.

```razor
@code {
    private string _activeTab = "Overview";
    private string[] _tabs = ["Overview", "Details", "Logs"];

    private void SelectTab(string tab) => _activeTab = tab;
}

<Rows>
    {{* Tab bar *}}
    <Columns>
        @foreach (var tab in _tabs)
        {
            <TextButton Content="@tab"
                        OnClick="@(() => SelectTab(tab))"
                        BackgroundColor="@(tab == _activeTab ? Color.Blue : Color.Grey)"
                        FocusedColor="Color.DeepSkyBlue1" />
        }
    </Columns>

    {{* Tab content *}}
    @if (_activeTab == "Overview")
    {
        <Panel Title="Overview"><Markup Content="Overview content..." /></Panel>
    }
    else if (_activeTab == "Details")
    {
        <Panel Title="Details"><Markup Content="Details content..." /></Panel>
    }
    else if (_activeTab == "Logs")
    {
        <Panel Title="Logs"><Markup Content="Log content..." /></Panel>
    }
</Rows>
```

Visual result:

```
[ Overview ] [ Details ] [ Logs ]
┌─────────────────────────────────┐
│ Overview content...             │
└─────────────────────────────────┘
```

## Option B: Keyboard-driven tabs with arrow navigation

For a more TUI-native feel where tabs switch via `←`/`→` without clicking, add a `@onkeydown` handler to the tab bar container and cycle `_activeTab` based on key input. RazorConsole exposes `KeyboardEventArgs` via focusable components.

## Option C: Multipage with `Router` (if tabs mean "pages")

If tabs are distinct views/pages, use Blazor's `<Router>` with `NavigationManager.NavigateTo("/tab-name")`. The tab bar still uses `TextButton`s, but navigation is handled by the router rather than local state. See the [MultipageApp example](https://github.com/RazorConsole/RazorConsole/tree/main/examples/MultipageApp).

```razor
@inject NavigationManager Nav

<TextButton Content="Settings"
            OnClick="@(() => Nav.NavigateTo("/settings"))" />
```

## Key constraints

- **Focus management**: `TextButton` participates in tab-order focus via `FocusOrder`. Assign it carefully so keyboard navigation across the tab bar feels natural.
- **No built-in active indicator**: Active state is controlled entirely through `BackgroundColor`/styling — there is no automatic underline style like in a web tabs component.
- **Pure Spectre.Console** (without RazorConsole): You would need `AnsiConsole.Live()` plus manual keyboard polling — significantly more complex. RazorConsole is the right tool for interactive TUIs.
