using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HelperFunctions {
    // Wrap text by line height
    // Thanks https://answers.unity.com/questions/190800/wrapping-a-textmesh-text.html
    public static string ResolveTextSize(string input) {
        const int lineLength = 47;
        // Split string by char " "         
        string[] words = input.Split(" "[0]);

        // Prepare result
        string result = "";

        // Temp line string
        string line = "";

        // for each all words        
        foreach (string s in words) {
            // Append current word into line
            string temp = line + " " + s;

            // If line length is bigger than lineLength
            if (temp.Length > lineLength) {

                // Append current line into result
                result += line + "\n";
                // Remain word append into new line
                line = s;
            }
            // Append current word into current line
            else {
                line = temp;
            }
        }

        // Append last line into result        
        result += line;

        // Remove first " " char
        return result.Substring(1, result.Length - 1);
    }
}
