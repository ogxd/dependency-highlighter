using System;

namespace Ogxd.ProjectCurator {
  internal static class StringUtility
  {
    // https://stackoverflow.com/a/7170953/317135
    public static string TrimEnd(
      this string input,
      string suffixToRemove,
      StringComparison comparisonType = StringComparison.CurrentCulture
    ) {
      if (suffixToRemove != null && input.EndsWith(suffixToRemove, comparisonType)) {
        return input[0..^suffixToRemove.Length];
      }
      return input;
    }
  }
}
