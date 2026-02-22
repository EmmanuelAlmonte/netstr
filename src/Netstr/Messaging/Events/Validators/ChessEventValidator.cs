using Netstr.Messaging.Models;
using System.Text.RegularExpressions;

namespace Netstr.Messaging.Events.Validators
{
    /// <summary>
    /// Validates NIP-64 Chess events with PGN content.
    /// </summary>
    public class ChessEventValidator : IEventValidator
    {
        private const string InvalidPgnFormat = "invalid: PGN format is not valid";
        private const string InvalidChessContent = "invalid: chess content is empty or malformed";

        // Basic PGN validation patterns
        private static readonly Regex PgnHeaderPattern = new(@"^\[([A-Za-z0-9_]+)\s+""([^""]*)""\]\s*$", RegexOptions.Compiled);
        private static readonly Regex PgnMovePattern = new(@"^[1-9]\d*\.(\s+[NBRQK]?[a-h]?[1-8]?x?[a-h][1-8](?:=[NBRQ])?[+#]?|O-O(?:-O)?[+#]?|\*|1-0|0-1|1/2-1/2)", RegexOptions.Compiled);
        private static readonly Regex PgnResultPattern = new(@"(\*|1-0|0-1|1/2-1/2)$", RegexOptions.Compiled);

        public string? Validate(Event e, ClientContext context)
        {
            // Only validate chess events
            if (e.Kind != (long)EventKind.Chess)
            {
                return null;
            }

            // Check if content is empty
            if (string.IsNullOrWhiteSpace(e.Content))
            {
                return InvalidChessContent;
            }

            // Basic PGN format validation
            if (!IsValidPgnFormat(e.Content))
            {
                return InvalidPgnFormat;
            }

            return null;
        }

        private static bool IsValidPgnFormat(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var normalizedContent = content.Trim();

            // Handle simple cases first
            if (normalizedContent == "*")
            {
                return true; // Unknown result, valid PGN
            }

            // Check for basic move patterns like "1. e4 *" or "1. e4 e5 2. Nf3 *"
            if (PgnMovePattern.IsMatch(normalizedContent))
            {
                return true;
            }

            // For more complex PGN with headers and moves
            var lines = normalizedContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length == 0)
            {
                return false;
            }

            bool hasValidStructure = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    continue;
                }

                // Check for PGN headers [Tag "Value"]
                if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
                {
                    if (PgnHeaderPattern.IsMatch(trimmedLine))
                    {
                        hasValidStructure = true;
                    }
                    continue;
                }

                // Check for move text
                if (!trimmedLine.StartsWith('['))
                {
                    // Basic validation for moves or result
                    if (ContainsValidMoveOrResult(trimmedLine))
                    {
                        hasValidStructure = true;
                    }
                }
            }

            return hasValidStructure;
        }

        private static bool ContainsValidMoveOrResult(string moveText)
        {
            // Check for game result
            if (PgnResultPattern.IsMatch(moveText))
            {
                return true;
            }

            // Check for basic move patterns
            if (moveText.Contains("1.") || moveText.Contains("2.") || moveText.Contains("e4") || 
                moveText.Contains("e5") || moveText.Contains("Nf3") || moveText.Contains("O-O"))
            {
                return true;
            }

            // Check for basic algebraic notation patterns
            var algebraicPattern = new Regex(@"[a-h][1-8]|[NBRQK][a-h]?[1-8]?x?[a-h][1-8]|O-O(-O)?", RegexOptions.Compiled);
            return algebraicPattern.IsMatch(moveText);
        }
    }
}