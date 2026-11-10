using ContractGuard.Application.Reports;
using ContractGuard.Domain.Analysis;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContractGuard.Infrastructure.Reports;

// FR-EXP-001 with QuestPDF (Community license: free under USD 1M revenue, fits a student project).
// The font is embedded so Vietnamese renders the same on Windows and in the Linux container.
internal sealed class PdfReportRenderer : IReportRenderer
{
    private const string FontFamily = "Be Vietnam Pro";

    static PdfReportRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = false;
        foreach (var weight in new[] { "Regular", "SemiBold" })
        {
            using var font = typeof(PdfReportRenderer).Assembly.GetManifestResourceStream($"BeVietnamPro-{weight}.ttf")
                ?? throw new InvalidOperationException($"Embedded font BeVietnamPro-{weight}.ttf is missing.");
            QuestPDF.Drawing.FontManager.RegisterFontFromStream(font);
        }
    }

    public ReportFormat Format => ReportFormat.Pdf;

    public string ContentType => "application/pdf";

    public byte[] Render(ReportData data)
    {
        var t = data.Text;
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(style => style.FontFamily(FontFamily).FontSize(10).LineHeight(1.4f));

            page.Header().PaddingBottom(12).Column(header =>
            {
                header.Item().Text(t.Title).FontSize(16).SemiBold();
                header.Item().Text($"{data.ContractName} · V{data.VersionNumber}").FontSize(11);
                header.Item().Text($"{t.GeneratedAt}: {t.DateTime(data.GeneratedAt)}").FontColor(Colors.Grey.Darken1);
            });

            page.Content().Column(content =>
            {
                content.Spacing(14);
                content.Item().Element(c => ContractInfo(c, data));
                content.Item().Element(c => Scores(c, data));
                content.Item().Element(c => FindingList(c, data));
                content.Item().Element(c => Checklist(c, data));
                content.Item().Element(c => Section(c, t.Trace).Text(
                    $"schema {data.Trace.SchemaVersion} · model {data.Trace.ModelVersion ?? "-"} · prompt {data.Trace.PromptVersion ?? "-"} · rule {data.Trace.RuleVersion ?? "-"}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1));
                content.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(8).Text(t.Disclaimer).Italic().FontSize(9);
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        })).GeneratePdf();
    }

    private static void ContractInfo(IContainer container, ReportData data)
    {
        var t = data.Text;
        var m = data.Metadata;
        var rows = new List<(string Label, string Value)>
        {
            (t.ContractName, data.ContractName),
            (t.Version, $"V{data.VersionNumber} · {data.FileName}"),
            (t.ApprovalStatus, t.ApprovalName(data.ApprovalStatus))
        };
        if (data.Context is { } context)
        {
            rows.Add((t.OwnParty, t.OwnPartyName(context.OwnParty)));
            rows.Add((t.Counterparty, context.CounterpartyName ?? t.Unknown));
        }

        rows.Add((t.Parties, m is { Parties.Count: > 0 } ? string.Join("; ", m.Parties) : t.Unknown));
        rows.Add((t.ContractType, m?.ContractType is { } type ? t.ContractTypeName(type) : t.Unknown));
        rows.Add((t.ContractValue, t.Money(m?.ContractValue, m?.Currency)));
        rows.Add((t.EffectiveDate, t.Date(m?.EffectiveDate)));
        rows.Add((t.ExpiryDate, t.Date(m?.ExpiryDate)));
        rows.Add((t.GoverningLaw, m?.GoverningLaw ?? t.Unknown));

        Section(container, t.ContractInfo).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(130);
                columns.RelativeColumn();
            });
            foreach (var (label, value) in rows)
            {
                table.Cell().PaddingVertical(2).Text(label).FontColor(Colors.Grey.Darken2);
                table.Cell().PaddingVertical(2).Text(value);
            }
        });
    }

    private static void Scores(IContainer container, ReportData data)
    {
        var t = data.Text;
        Section(container, t.OverallScore).Column(column =>
        {
            column.Spacing(6);
            if (data.Score is { } score)
            {
                column.Item().Text(text =>
                {
                    text.Span($"{score.OverallScore}/100 · ").FontSize(14).SemiBold();
                    text.Span(t.LevelName(score.RiskLevel)).FontSize(14).SemiBold().FontColor(LevelColor(score.RiskLevel));
                });
                column.Item().Text(t.DomainScores).SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(4);
                    });
                    HeaderCells(table, t.Domain, t.Score, t.Level);
                    foreach (var domain in score.Domains)
                    {
                        table.Cell().Element(Row).Text(t.DomainName(domain.Domain));
                        if (domain.Applicable && domain.Score is { } value && domain.RiskLevel is { } level)
                        {
                            table.Cell().Element(Row).Text(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                            table.Cell().Element(Row).Text(t.LevelName(level) + (domain.ReEnabled ? $" · {t.ReEnabled}" : string.Empty)).FontColor(LevelColor(level));
                        }
                        else
                        {
                            table.Cell().Element(Row).Text("N/A");
                            table.Cell().Element(Row).Text($"{t.NotApplicable}: {domain.NaReason}").FontColor(Colors.Grey.Darken1);
                        }
                    }
                });
            }
            else
            {
                column.Item().Text(t.NoScore).Italic();
            }

            column.Item().Text(t.Recommendation).SemiBold();
            column.Item().Text(t.SeveritySummary(data.SeverityCounts[Severity.Critical], data.SeverityCounts[Severity.High]));
            if (!string.IsNullOrWhiteSpace(data.OverallRecommendation))
            {
                column.Item().Text(data.OverallRecommendation);
            }
        });
    }

    private static void FindingList(IContainer container, ReportData data)
    {
        var t = data.Text;
        Section(container, $"{t.Findings} ({data.Findings.Count})").Column(column =>
        {
            column.Spacing(10);
            if (data.Scope == ReportScope.Severe)
            {
                column.Item().Text(t.ScopeHighOnly).Italic();
            }

            if (data.Findings.Count == 0)
            {
                column.Item().Text(t.NoFindings);
            }

            foreach (var finding in data.Findings)
            {
                column.Item().BorderLeft(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingLeft(8).Column(item =>
                {
                    item.Spacing(2);
                    item.Item().Text(text =>
                    {
                        text.Span(t.SeverityName(finding.Severity)).SemiBold().FontColor(LevelColor((RiskLevel)(int)finding.Severity));
                        text.Span($" · {t.DomainName(finding.PrimaryDomain)}{(finding.CheckCode is null ? string.Empty : $" · {finding.CheckCode}")}").FontColor(Colors.Grey.Darken1);
                    });
                    item.Item().Text(finding.Title).SemiBold();
                    item.Item().Text(finding.Kind == FindingKind.MissingClause ? t.MissingClause : $"{t.Clause}: {data.ClauseName(finding.ClauseKey)}").FontColor(Colors.Grey.Darken2);
                    foreach (var evidence in finding.Evidence)
                    {
                        item.Item().Text($"{t.Evidence}: “{evidence.Quote}”").Italic();
                    }

                    if (finding.MissingDescription is not null)
                    {
                        item.Item().Text($"{t.SearchedFor}: {finding.MissingDescription}");
                    }

                    Labeled(item, t.Why, finding.Why);
                    Labeled(item, t.Impact, finding.Impact);
                    Labeled(item, t.WhatToDo, finding.Recommendation);
                    if (finding.References.Count > 0)
                    {
                        Labeled(item, t.References, string.Join("; ", finding.References.Select(r => $"{r.Source} {r.ReferenceNumber}")));
                    }

                    var handling = t.HandlingName(finding.HandlingStatus) + (finding.HandlingNote is null ? string.Empty : $" · {finding.HandlingNote}");
                    Labeled(item, t.Handling, handling);
                });
            }
        });
    }

    private static void Checklist(IContainer container, ReportData data)
    {
        var t = data.Text;
        Section(container, t.Checklist).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(44);
                columns.RelativeColumn(5);
                columns.ConstantColumn(60);
                columns.RelativeColumn(3);
            });
            HeaderCells(table, t.Check, string.Empty, t.Status, t.Clause);
            foreach (var check in data.Checks)
            {
                table.Cell().Element(Row).Text(check.CheckCode);
                table.Cell().Element(Row).Text(t.CheckName(check.CheckCode));
                table.Cell().Element(Row).Text(t.CheckStatusName(check.Status)).SemiBold();
                table.Cell().Element(Row).Text(string.Join("; ", check.ClauseKeys.Select(data.ClauseName)));
            }
        });
    }

    private static IContainer Section(IContainer container, string title)
    {
        var column = new List<IContainer>();
        container.Column(c =>
        {
            c.Spacing(6);
            c.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingBottom(2).Text(title).FontSize(12).SemiBold();
            column.Add(c.Item());
        });
        return column[0];
    }

    private static void Labeled(ColumnDescriptor column, string label, string value) =>
        column.Item().Text(text =>
        {
            text.Span($"{label}: ").SemiBold();
            text.Span(value);
        });

    private static void HeaderCells(TableDescriptor table, params string[] titles)
    {
        table.Header(header =>
        {
            foreach (var title in titles)
            {
                header.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Medium).PaddingVertical(3).Text(title).SemiBold();
            }
        });
    }

    private static IContainer Row(IContainer cell) => cell.BorderBottom(0.25f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingRight(4);

    // Close to the severity tokens of DESIGN §2, darkened enough to read on white paper.
    private static string LevelColor(RiskLevel level) => level switch
    {
        RiskLevel.Critical => "#8F2A24",
        RiskLevel.High => "#A8481B",
        RiskLevel.Medium => "#7E5A12",
        _ => "#3B6B4A"
    };
}
