#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.Build;

public static class PreprocessorDefinesManager
{
    public static void AddDefineSymbol(string symbol)
    {
        string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        if (!currentSymbols.Contains(symbol))
        {
            string newSymbols = currentSymbols + (string.IsNullOrEmpty(currentSymbols) ? "" : ";") + symbol;
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone, newSymbols);
            Debug.Log($"Added define symbol: {symbol}");
        }
        else
        {
            Debug.Log($"Define symbol already exists: {symbol}");
        }
    }

    public static void RemoveDefineSymbol(string symbol)
    {
        string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        string[] symbols = currentSymbols.Split(';');
        string newSymbols = string.Join(";", symbols.Where(s => s != symbol));
        PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone, newSymbols);
        Debug.Log($"Removed define symbol: {symbol}");
    }
}
#endif
