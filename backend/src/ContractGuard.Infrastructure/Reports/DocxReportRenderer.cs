using ContractGuard.Application.Reports;
using ContractGuard.Domain.Analysis;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ContractGuard.Infrastructure.Reports;

// FR-EXP-003: the same sections as the PDF, as an editable Word file. Plain paragraphs and tables with
// built-in heading styles, so the user can restyle or add their own notes.
internal sealed class DocxReportRenderer : IReportRenderer
{
    public ReportFormat Format => ReportFormat.Docx;

    public string ContentType => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public byte[] Render(ReportData data)
    {
        var t = data.Text;
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            AddStyles(main);
            var body = new Body();
            main.Document = new Document(body);

            body.Append(Heading(t.Title, 1));
            body.Append(Paragraph($"{data.ContractName} · V{data.VersionNumber}"));
            body.Append(Paragraph($"{t.GeneratedAt}: {t.DateTime(data.GeneratedAt)}"));

            body.Append(Heading(t.ContractInfo, 2));
            var m = data.Metadata;
            var info = new List<string[]>
            {
                new[] { t.ContractName, data.ContractName },
                new[] { t.Version, $"V{data.VersionNumber} · {data.FileName}" },
                new[] { t.ApprovalStatus, t.ApprovalName(data.ApprovalStatus) }
            };
            if (data.Context is { } context)
            {
                info.Add([t.OwnParty, t.OwnPartyName(context.OwnParty)]);
                info.Add([t.Counterparty, context.CounterpartyName ?? t.Unknown]);
            }

            info.Add([t.Parties, m is { Parties.Count: > 0 } ? string.Join("; ", m.Parties) : t.Unknown]);
            info.Add([t.ContractType, m?.ContractType is { } type ? t.ContractTypeName(type) : t.Unknown]);
            info.Add([t.ContractValue, t.Money(m?.ContractValue, m?.Currency)]);
            info.Add([t.EffectiveDate, t.Date(m?.EffectiveDate)]);
            info.Add([t.ExpiryDate, t.Date(m?.ExpiryDate)]);
            info.Add([t.GoverningLaw, m?.GoverningLaw ?? t.Unknown]);
            body.Append(Table(null, info));

            body.Append(Heading(t.OverallScore, 2));
            if (data.Score is { } score)
            {
                body.Append(Paragraph($"{score.OverallScore}/100 · {t.LevelName(score.RiskLevel)}", bold: true));
                body.Append(Heading(t.DomainScores, 3));
                body.Append(Table(
                    [t.Domain, t.Score, t.Level],
                    [.. score.Domains.Select(d => d.Applicable && d.Score is { } value && d.RiskLevel is { } level
                        ? new[] { t.DomainName(d.Domain), value.ToString(System.Globalization.CultureInfo.InvariantCulture), t.LevelName(level) + (d.ReEnabled ? $" · {t.ReEnabled}" : string.Empty) }
                        : [t.DomainName(d.Domain), "N/A", $"{t.NotApplicable}: {d.NaReason}"])]));
            }
            else
            {
                body.Append(Paragraph(t.NoScore));
            }

            body.Append(Heading(t.Recommendation, 3));
            body.Append(Paragraph(t.SeveritySummary(data.SeverityCounts[Severity.Critical], data.SeverityCounts[Severity.High])));
            if (!string.IsNullOrWhiteSpace(data.OverallRecommendation))
            {
                body.Append(Paragraph(data.OverallRecommendation));
            }

            body.Append(Heading($"{t.Findings} ({data.Findings.Count})", 2));
            if (data.Scope == ReportScope.Severe)
            {
                body.Append(Paragraph(t.ScopeHighOnly));
            }

            if (data.Findings.Count == 0)
            {
                body.Append(Paragraph(t.NoFindings));
            }

            foreach (var finding in data.Findings)
            {
                body.Append(Heading(finding.Title, 3));
                body.Append(Paragraph($"{t.SeverityName(finding.Severity)} · {t.DomainName(finding.PrimaryDomain)}{(finding.CheckCode is null ? string.Empty : $" · {finding.CheckCode}")}", bold: true));
                body.Append(Paragraph(finding.Kind == FindingKind.MissingClause ? t.MissingClause : $"{t.Clause}: {data.ClauseName(finding.ClauseKey)}"));
                foreach (var evidence in finding.Evidence)
                {
                    body.Append(Paragraph($"{t.Evidence}: “{evidence.Quote}”", italic: true));
                }

                if (finding.MissingDescription is not null)
                {
                    body.Append(Labeled(t.SearchedFor, finding.MissingDescription));
                }

                body.Append(Labeled(t.Why, finding.Why));
                body.Append(Labeled(t.Impact, finding.Impact));
                body.Append(Labeled(t.WhatToDo, finding.Recommendation));
                if (finding.References.Count > 0)
                {
                    body.Append(Labeled(t.References, string.Join("; ", finding.References.Select(r => $"{r.Source} {r.ReferenceNumber}"))));
                }

                body.Append(Labeled(t.Handling, t.HandlingName(finding.HandlingStatus) + (finding.HandlingNote is null ? string.Empty : $" · {finding.HandlingNote}")));
            }

            body.Append(Heading(t.Checklist, 2));
            body.Append(Table(
                [t.Check, string.Empty, t.Status, t.Clause],
                [.. data.Checks.Select(c => new[] { c.CheckCode, t.CheckName(c.CheckCode), t.CheckStatusName(c.Status), string.Join("; ", c.ClauseKeys.Select(data.ClauseName)) })]));

            body.Append(Heading(t.Trace, 3));
            body.Append(Paragraph($"schema {data.Trace.SchemaVersion} · model {data.Trace.ModelVersion ?? "-"} · prompt {data.Trace.PromptVersion ?? "-"} · rule {data.Trace.RuleVersion ?? "-"}"));
            body.Append(Paragraph(t.Disclaimer, italic: true));

            body.Append(new SectionProperties(
                new PageSize { Width = 11906, Height = 16838 }, // A4 in twentieths of a point
                new PageMargin { Top = 1134, Bottom = 1134, Left = 1134, Right = 1134, Header = 708, Footer = 708, Gutter = 0 }));
            main.Document.Save();
        }

        return stream.ToArray();
    }

    private static Paragraph Heading(string text, int level) =>
        new(new ParagraphProperties(new ParagraphStyleId { Val = $"Heading{level}" }), new Run(new Text(text)));

    private static Paragraph Paragraph(string text, bool bold = false, bool italic = false)
    {
        var properties = new RunProperties();
        if (bold)
        {
            properties.Append(new Bold());
        }

        if (italic)
        {
            properties.Append(new Italic());
        }

        return new Paragraph(new Run(properties, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private static Paragraph Labeled(string label, string value) =>
        new(
            new Run(new RunProperties(new Bold()), new Text($"{label}: ") { Space = SpaceProcessingModeValues.Preserve }),
            new Run(new Text(value) { Space = SpaceProcessingModeValues.Preserve }));

    private static Table Table(string[]? header, IEnumerable<string[]> rows)
    {
        var border = BorderValues.Single;
        var table = new Table(new TableProperties(
            new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
            new TableBorders(
                new TopBorder { Val = border, Size = 4 },
                new BottomBorder { Val = border, Size = 4 },
                new InsideHorizontalBorder { Val = border, Size = 4 })));
        if (header is not null)
        {
            table.Append(Row(header, bold: true));
        }

        foreach (var row in rows)
        {
            table.Append(Row(row, bold: false));
        }

        return table;
    }

    private static TableRow Row(string[] cells, bool bold) =>
        new([.. cells.Select(cell => new TableCell(Paragraph(cell, bold)))]);

    // Default font with Vietnamese glyphs on every Office install, and real heading styles.
    private static void AddStyles(MainDocumentPart main)
    {
        var styles = main.AddNewPart<StyleDefinitionsPart>();
        styles.Styles = new Styles(
            new DocDefaults(new RunPropertiesDefault(new RunPropertiesBaseStyle(
                new RunFonts { Ascii = "Arial", HighAnsi = "Arial", ComplexScript = "Arial" },
                new FontSize { Val = "20" }))),
            HeadingStyle(1, 32),
            HeadingStyle(2, 26),
            HeadingStyle(3, 22));
        styles.Styles.Save();
    }

    private static Style HeadingStyle(int level, int halfPoints) =>
        new(
            new StyleName { Val = $"heading {level}" },
            new BasedOn { Val = "Normal" },
            new PrimaryStyle(),
            new StyleParagraphProperties(new KeepNext(), new SpacingBetweenLines { Before = "240", After = "80" }, new OutlineLevel { Val = level - 1 }),
            new StyleRunProperties(new Bold(), new FontSize { Val = halfPoints.ToString(System.Globalization.CultureInfo.InvariantCulture) }))
        {
            Type = StyleValues.Paragraph,
            StyleId = $"Heading{level}"
        };
}
