using Microsoft.UI.Xaml.Controls;
using PrintManagement.Dialogs;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace PrintManagement.Services;

internal static class CredentialsService
{
    // Credentials entered this session, even when "Remember me" is unchecked.
    // Prevents a second prompt for the same server (e.g. printers then drivers).
    private static readonly ConcurrentDictionary<string, (string User, string Domain, string Password)> SessionCache
        = new(StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string serverName) => serverName.Trim().TrimStart('\\');

    private static string GetResourceKey(string serverName) => $"PrintManagement_{Normalize(serverName)}";

    private static (string User, string Domain, string Password) Split(string fullUserName, string password)
    {
        if (fullUserName.Contains('\\'))
        {
            var parts = fullUserName.Split('\\', 2);
            return (parts[1], parts[0], password);
        }
        return (fullUserName, "", password);
    }

    // ───────────── Read ─────────────

    public static (string User, string Domain, string Password)? GetStoredCredentials(string serverName)
    {
        if (SessionCache.TryGetValue(Normalize(serverName), out var cached))
            return cached;


        try
        {
            var vault = new PasswordVault();
            var credList = vault.FindAllByResource(GetResourceKey(serverName));
            if (credList.Count > 0)
            {
                var cred = credList[0];
                cred.RetrievePassword();
                return Split(cred.UserName, cred.Password);
            }
        }
        catch (Exception)
        {
            // FindAllByResource throws "element not found" when nothing is stored; that's the normal case.
        }
        return null;
    }

    // ───────────── RemoveFromVault  ─────────────

    // Unconditional wipe, used only when saving a replacement (PromptCoreAsync).
    private static void RemoveFromVault(string serverName)
    {
        Logger.Log($"RemoveFromVault('{serverName}') called from:\n{Environment.StackTrace}");
        try
        {
            var vault = new PasswordVault();
            foreach (var old in vault.FindAllByResource(GetResourceKey(serverName)))
                vault.Remove(old);
        }
        catch { }
    }

    // ───────────── Prompt ─────────────

    public static Task<(string User, string Domain, string Password)?> PromptAndSaveCredentialsAsync(
        string serverName, string? error = null)
        => OnUiThreadAsync(() => PromptCoreAsync(serverName, error));

    private static async Task<(string User, string Domain, string Password)?> PromptCoreAsync(
        string serverName, string? error)
    {
        var xamlRoot = App.MainWindow?.Content?.XamlRoot
            ?? throw new InvalidOperationException("Main window is not ready; cannot show the credentials dialog.");

        var dialog = new CustomLoginDialog(serverName, xamlRoot);
        if (error is not null)
            dialog.ShowError(error);

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return null; // user cancelled

        string fullUserName = dialog.Username.Trim();
        string password = dialog.Password;
        var creds = Split(fullUserName, password);

        SessionCache[Normalize(serverName)] = creds;

        // Replace any old entry first, otherwise FindAllByResource()[0] keeps returning the stale one.
        RemoveFromVault(serverName);
        if (dialog.RememberMe)
        {
            try
            {
                new PasswordVault().Add(new PasswordCredential(GetResourceKey(serverName), fullUserName, password));
            }
            catch (Exception ex)
            {
                Logger.Warning($"Could not save credentials for '{serverName}': {ex.Message}");
            }
        }

        return creds;
    }

    // ───────────── UI thread helper ─────────────

    private static Task<T> OnUiThreadAsync<T>(Func<Task<T>> work)
    {
        var queue = App.MainWindow?.DispatcherQueue
            ?? throw new InvalidOperationException("Main window is not available.");

        if (queue.HasThreadAccess)
            return work();

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool queued = queue.TryEnqueue(async () =>
        {
            try { tcs.SetResult(await work()); }
            catch (Exception ex) { tcs.SetException(ex); }
        });

        if (!queued)
            tcs.SetException(new InvalidOperationException("UI thread is unavailable."));

        return tcs.Task;
    }
}