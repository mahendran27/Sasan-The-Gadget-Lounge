namespace SasanERP.Core.Specifications;

/// <summary>
/// Breakdown of Indian GST tax components for standard goods
/// </summary>
public record GstTaxBreakdown(
    decimal TaxableValue,
    decimal RatePercentage,
    decimal CgstPercentage,
    decimal CgstAmount,
    decimal SgstPercentage,
    decimal SgstAmount,
    decimal IgstPercentage,
    decimal IgstAmount,
    decimal TotalTax,
    decimal GrossAmount
);

/// <summary>
/// Computation details under Rule 32(5) of CGST Rules 2017 (GST Margin Scheme for Second-Hand Goods)
/// </summary>
public record MarginSchemeTaxBreakdown(
    decimal SellingPrice,
    decimal PurchaseCost,
    decimal GrossMargin,
    bool IsMarginPositive,
    decimal TaxableMarginValue,
    decimal RatePercentage,
    decimal CgstPercentage,
    decimal CgstAmount,
    decimal SgstPercentage,
    decimal SgstAmount,
    decimal TotalMarginTax,
    string StatutoryDeclaration
);

/// <summary>
/// Indian GST Accounting and Rule 32(5) Margin Calculator
/// </summary>
public static class GstCalculator
{
    public const string Rule32_5_StatutoryText = 
        "Invoice issued under Rule 32(5) of CGST Rules, 2017. Supply of pre-owned/second-hand goods under Margin Scheme. No Input Tax Credit (ITC) has been availed on purchase.";

    /// <summary>
    /// Computes standard GST breakdown when price is inclusive of GST (Standard Indian Retail POS)
    /// </summary>
    public static GstTaxBreakdown ComputeTaxInclusive(decimal grossPrice, decimal gstRate, bool isInterstate = false)
    {
        if (grossPrice <= 0 || gstRate <= 0)
        {
            return new GstTaxBreakdown(grossPrice, gstRate, 0, 0, 0, 0, 0, 0, 0, grossPrice);
        }

        // Taxable = Gross / (1 + Rate / 100)
        decimal taxableValue = Math.Round(grossPrice / (1m + (gstRate / 100m)), 2, MidpointRounding.AwayFromZero);
        decimal totalTax = grossPrice - taxableValue;

        if (isInterstate)
        {
            return new GstTaxBreakdown(
                TaxableValue: taxableValue,
                RatePercentage: gstRate,
                CgstPercentage: 0,
                CgstAmount: 0,
                SgstPercentage: 0,
                SgstAmount: 0,
                IgstPercentage: gstRate,
                IgstAmount: totalTax,
                TotalTax: totalTax,
                GrossAmount: grossPrice
            );
        }
        else
        {
            decimal halfRate = gstRate / 2m;
            decimal cgst = Math.Round(totalTax / 2m, 2, MidpointRounding.AwayFromZero);
            decimal sgst = totalTax - cgst; // Balance ensures no rounding variance

            return new GstTaxBreakdown(
                TaxableValue: taxableValue,
                RatePercentage: gstRate,
                CgstPercentage: halfRate,
                CgstAmount: cgst,
                SgstPercentage: halfRate,
                SgstAmount: sgst,
                IgstPercentage: 0,
                IgstAmount: 0,
                TotalTax: totalTax,
                GrossAmount: grossPrice
            );
        }
    }

    /// <summary>
    /// Computes GST under Rule 32(5) Margin Scheme strictly on (Selling Price - Purchase Cost)
    /// </summary>
    public static MarginSchemeTaxBreakdown ComputeMarginScheme(decimal sellingPrice, decimal purchaseCost, decimal gstRate, bool isInterstate = false)
    {
        decimal margin = sellingPrice - purchaseCost;

        if (margin <= 0 || gstRate <= 0)
        {
            // Under Rule 32(5) proviso: where margin is negative or zero, it shall be ignored and Tax is Zero.
            return new MarginSchemeTaxBreakdown(
                SellingPrice: sellingPrice,
                PurchaseCost: purchaseCost,
                GrossMargin: margin,
                IsMarginPositive: false,
                TaxableMarginValue: 0,
                RatePercentage: gstRate,
                CgstPercentage: 0,
                CgstAmount: 0,
                SgstPercentage: 0,
                SgstAmount: 0,
                TotalMarginTax: 0,
                StatutoryDeclaration: Rule32_5_StatutoryText
            );
        }

        // Margin is treated as tax-inclusive profit margin
        decimal taxableMargin = Math.Round(margin / (1m + (gstRate / 100m)), 2, MidpointRounding.AwayFromZero);
        decimal totalTax = margin - taxableMargin;
        decimal halfRate = gstRate / 2m;
        decimal cgst = Math.Round(totalTax / 2m, 2, MidpointRounding.AwayFromZero);
        decimal sgst = totalTax - cgst;

        return new MarginSchemeTaxBreakdown(
            SellingPrice: sellingPrice,
            PurchaseCost: purchaseCost,
            GrossMargin: margin,
            IsMarginPositive: true,
            TaxableMarginValue: taxableMargin,
            RatePercentage: gstRate,
            CgstPercentage: halfRate,
            CgstAmount: cgst,
            SgstPercentage: halfRate,
            SgstAmount: sgst,
            TotalMarginTax: totalTax,
            StatutoryDeclaration: Rule32_5_StatutoryText
        );
    }
}
