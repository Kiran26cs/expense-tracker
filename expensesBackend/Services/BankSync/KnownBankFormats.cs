namespace ExpensesBackend.API.Services.BankSync;

public static class KnownBankFormats
{
    public static readonly IReadOnlyList<BankColumnMap> All = new List<BankColumnMap>
    {
        // ── HDFC ─────────────────────────────────────────────────────────────────
        // Headers: Date | Narration | Value Dat | Debit Amount | Credit Amount | Chq/Ref Number | Closing Balance
        new()
        {
            BankName             = "HDFC",
            IdentifyingColumns   = ["Narration"],
            DateColumnNames      = ["Date"],
            DescriptionColumnNames = ["Narration"],
            DebitColumnNames     = ["Debit Amount"],
            CreditColumnNames    = ["Credit Amount"],
            DateFormats          = ["dd/MM/yy", "dd/MM/yyyy", "d/M/yyyy", "d/M/yy"]
        },

        // ── ICICI ────────────────────────────────────────────────────────────────
        // Format A (Net Banking CSV): Transaction Date | Value Date | Description | Ref No./Cheque No. | Debit | Credit | Balance
        new()
        {
            BankName             = "ICICI",
            IdentifyingColumns   = ["Transaction Date", "Ref No./Cheque No."],
            DateColumnNames      = ["Transaction Date"],
            DescriptionColumnNames = ["Description"],
            DebitColumnNames     = ["Debit"],
            CreditColumnNames    = ["Credit"],
            DateFormats          = ["dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy"]
        },
        // Format B (iMobile / Opportunity / iSave / Detailed Statement XLS):
        // S No. | Value Date | Transaction Date | Cheque Number | Transaction Remarks | Withdrawal Amount(INR) | Deposit Amount(INR) | Balance(INR)
        // Note: Excel headers may use Alt+Enter (collapsed to space by NormalizeCellValue), so "Withdrawal\nAmount(INR)" → "Withdrawal Amount(INR)"
        new()
        {
            BankName             = "ICICI",
            IdentifyingColumns   = ["Transaction Remarks"],
            DateColumnNames      = ["Transaction Date", "Value Date"],
            DescriptionColumnNames = ["Transaction Remarks"],
            DebitColumnNames     = [
                "Withdrawal Amount(INR)",      // actual header (no space before paren)
                "Withdrawal Amount (INR)",      // with space
                "Withdrawal Amount(INR )",     // trailing space inside paren
                "Withdrawal Amount (INR )",    // both spaces
                "Debit", "Debit Amount"
            ],
            CreditColumnNames    = [
                "Deposit Amount(INR)",         // actual header (no space before paren)
                "Deposit Amount (INR)",         // with space
                "Deposit Amount(INR )",        // trailing space inside paren
                "Deposit Amount (INR )",       // both spaces
                "Credit", "Credit Amount"
            ],
            DateFormats          = ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd MMM yyyy", "d MMM yyyy"]
        },
        // Format C (Pockets/Corporate): Date | Particulars | Debit | Credit | Balance
        new()
        {
            BankName             = "ICICI",
            IdentifyingColumns   = ["Particulars"],
            DateColumnNames      = ["Date"],
            DescriptionColumnNames = ["Particulars"],
            DebitColumnNames     = ["Debit", "DR"],
            CreditColumnNames    = ["Credit", "CR"],
            DateFormats          = ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy"]
        },

        // ── SBI ──────────────────────────────────────────────────────────────────
        // Headers: Txn Date | Value Date | Description | Ref No./Cheque No. | Debit | Credit | Balance
        new()
        {
            BankName             = "SBI",
            IdentifyingColumns   = ["Txn Date"],
            DateColumnNames      = ["Txn Date"],
            DescriptionColumnNames = ["Description"],
            DebitColumnNames     = ["Debit"],
            CreditColumnNames    = ["Credit"],
            DateFormats          = ["dd MMM yyyy", "d MMM yyyy", "dd-MMM-yyyy", "dd/MM/yyyy", "d/M/yyyy"]
        },

        // ── Axis ─────────────────────────────────────────────────────────────────
        // Headers: Tran Date | CHQNO | PARTICULARS | DR | CR | BAL
        new()
        {
            BankName             = "Axis",
            IdentifyingColumns   = ["PARTICULARS", "DR"],
            DateColumnNames      = ["Tran Date"],
            DescriptionColumnNames = ["PARTICULARS"],
            DebitColumnNames     = ["DR"],
            CreditColumnNames    = ["CR"],
            DateFormats          = ["dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy"]
        },

        // ── Kotak ────────────────────────────────────────────────────────────────
        // Headers: Transaction Date | Value Date | Description | Chq / Ref number | Debit Amount | Credit Amount | Balance
        new()
        {
            BankName             = "Kotak",
            IdentifyingColumns   = ["Chq / Ref number"],
            DateColumnNames      = ["Transaction Date"],
            DescriptionColumnNames = ["Description"],
            DebitColumnNames     = ["Debit Amount"],
            CreditColumnNames    = ["Credit Amount"],
            DateFormats          = ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy"]
        },

        // ── IndusInd ──────────────────────────────────────────────────────────────
        // Format A: Date | Transaction Details | Cheque No. | Debit | Credit | Balance
        new()
        {
            BankName             = "IndusInd",
            IdentifyingColumns   = ["Transaction Details"],
            DateColumnNames      = ["Date"],
            DescriptionColumnNames = ["Transaction Details"],
            DebitColumnNames     = ["Debit"],
            CreditColumnNames    = ["Credit"],
            DateFormats          = ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd MMM yyyy"]
        },
        // Format B: Value Date | Transaction Date | Cheque Number | Transaction Remarks | Withdrawal Amount(INR) | Deposit Amount(INR) | Balance(INR)
        new()
        {
            BankName             = "IndusInd",
            IdentifyingColumns   = ["Transaction Remarks"],
            DateColumnNames      = ["Transaction Date", "Value Date"],
            DescriptionColumnNames = ["Transaction Remarks"],
            DebitColumnNames     = ["Withdrawal Amount(INR)", "Withdrawal Amount (INR)", "Withdrawal Amount (INR )", "Debit"],
            CreditColumnNames    = ["Deposit Amount(INR)", "Deposit Amount (INR)", "Deposit Amount (INR )", "Credit"],
            DateFormats          = ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd MMM yyyy"]
        }
    };

    /// <summary>
    /// Auto-detect bank by checking identifying columns against headers.
    /// </summary>
    public static BankColumnMap? DetectBank(string[] headers)
    {
        var headerSet = new HashSet<string>(headers.Select(h => h.Trim()), StringComparer.OrdinalIgnoreCase);

        foreach (var map in All)
        {
            if (map.IdentifyingColumns.All(col => headerSet.Contains(col)))
                return map;
        }

        return null;
    }

    /// <summary>
    /// Try formats for a specific registered bank first, then fall back to auto-detect.
    /// This prevents the AI from misidentifying a statement when the user already told us the bank.
    /// </summary>
    public static BankColumnMap? DetectBankWithHint(string[] headers, string? bankNameHint)
    {
        var headerSet = new HashSet<string>(headers.Select(h => h.Trim()), StringComparer.OrdinalIgnoreCase);

        // Try the hinted bank's formats first (relaxed: only date+description columns need to exist)
        if (!string.IsNullOrEmpty(bankNameHint) && !bankNameHint.Equals("Other", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var map in All.Where(m => m.BankName.Equals(bankNameHint, StringComparison.OrdinalIgnoreCase)))
            {
                var dateExists = map.DateColumnNames.Any(c => headerSet.Contains(c));
                var descExists = map.DescriptionColumnNames.Any(c => headerSet.Contains(c));
                if (dateExists && descExists)
                    return map;
            }
        }

        // Fall back to auto-detect via identifying columns
        foreach (var map in All)
        {
            if (map.IdentifyingColumns.All(col => headerSet.Contains(col)))
                return map;
        }

        return null;
    }
}
