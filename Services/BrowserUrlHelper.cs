using System.Windows.Automation;

namespace RecallMate.Services;

/// <summary>
/// Best-effort URL extraction for browser windows using UI Automation. This
/// reads whatever text is currently in the address bar - it's not a network
/// hook, so it only works while the address bar is actually showing the URL
/// (not mid-edit with something else typed in) and requires the browser to
/// expose standard accessibility info, which Chrome/Edge/Firefox all do.
/// </summary>
internal static class BrowserUrlHelper
{
    private static readonly HashSet<string> BrowserProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome", "msedge", "firefox", "brave", "opera", "vivaldi"
    };

    public static bool IsBrowserProcess(string processName) => BrowserProcessNames.Contains(processName);

    public static string? TryGetAddressBarText(IntPtr hWnd)
    {
        try
        {
            var root = AutomationElement.FromHandle(hWnd);
            if (root is null)
                return null;

            var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
            var editControls = root.FindAll(TreeScope.Descendants, condition);

            foreach (AutomationElement element in editControls)
            {
                var name = element.Current.Name ?? string.Empty;
                var looksLikeAddressBar =
                    name.Contains("address", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("search", StringComparison.OrdinalIgnoreCase);

                if (!looksLikeAddressBar)
                    continue;

                if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var patternObj)
                    && patternObj is ValuePattern valuePattern)
                {
                    var value = valuePattern.Current.Value;
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
        }
        catch
        {
            // UI Automation throws for all sorts of transient reasons - element torn
            // down mid-query, no accessibility support, permission issues, etc.
            // Best effort only: just skip the URL for this snapshot.
        }

        return null;
    }
}
