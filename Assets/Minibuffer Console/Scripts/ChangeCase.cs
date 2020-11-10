/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis

*/

using System;
using System.Text.RegularExpressions;

namespace SeawispHunter.MinibufferConsole {


/**
   Convert between some conventional programmer cases.
 */
public static class ChangeCase {
  /**
   What kind of case convention should be used camelCase, PascalCase,
   kebab-case, or left as is?
  */
  public enum Case {
    /** camelCase */
    [Description("camelCase")]
    CamelCase,
    /** PascalCase */
    [Description("PascalCase")]
    PascalCase,
    /** kebab-case */
    [Description("kebab-case")]
    KebabCase,
    /* snake_case */
    [Description("snake_case")]
    SnakeCase,
    /** Leave it as is */
    [Description("Leave as is.")]
    AsIs,
  };

  public static string Convert(string input, Case _case) {
    string result = input;
    //var input = theInput.Replace(" ", string.Empty); // Get rid of spaces.
    switch (_case) {
      case Case.AsIs:
        break;
      case Case.PascalCase:
        result = KebabCaseToPascalCase(input);
        break;
      case Case.CamelCase:
        result = KebabCaseToCamelCase(input);
        break;
      case Case.KebabCase:
        result = CamelCaseToKebabCase(input);
        break;
      case Case.SnakeCase:
        result = CamelCaseToSnakeCase(input);
        break;
      default:
        result = null;
        throw new MinibufferException("Unknown case to convert " + input);
    }
    return result;
  }

  public static string CamelCaseToKebabCase(string input) {
    return Regex.Replace(input, " ?([A-Z]+)", m => {
        return (m.Index != 0 ? "-" : "") + m.Groups[1].Value.ToLower();
      }).Replace(" ", "-");
  }

  public static string CamelCaseToSnakeCase(string input) {
    return Regex.Replace(input, " ?([A-Z]+)", m => {
        return (m.Index != 0 ? "_" : "") + m.Groups[1].Value.ToLower();
      }).Replace(" ", "_").Replace("-", "_");
  }

  public static string PascalCaseToKebabCase(string input) {
    return Regex.Replace(input, " ?([A-Z]+)", m => {
        return (m.Index != 0 ? "-" : "") + m.Groups[1].Value.ToLower();
      }).Replace(" ", "-");
  }

  public static string KebabCaseToCamelCase(string input) {
    return
      Char.ToLower(input[0])
      + Regex.Replace(input.Substring(1), "[-_](.)", m => {
        return  m.Groups[1].Value.ToUpper();
      });
  }

  public static string KebabCaseToPascalCase(string input) {
    return Char.ToUpper(input[0])
      + Regex.Replace(input.Substring(1), "[-_](.)", m => {
          return  m.Groups[1].Value.ToUpper();
        });
  }
}

} // end of namespace SeawispHunter.MinibufferConsole
