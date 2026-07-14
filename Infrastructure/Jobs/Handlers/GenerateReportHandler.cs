using Application.Common.Constants;
using Application.Features.Reports.Jobs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Reports;
using Application.Interfaces.Services;
using Infrastructure.Jobs;
using Shared.Enums.System;

namespace Infrastructure.Jobs.Handlers;

internal sealed class GenerateReportHandler(
    IReportRepository              reportRepository,
    IEnumerable<IReportGenerator>  generators,
    IFileService                   fileService,
    IStorageUtility                storageUtility,
    IBackgroundJobService          backgroundJobService,
    INotificationPublisher         notificationPublisher) : JobHandlerBase<GenerateReportPayload>
{
    public override string JobType => JobTypes.GenerateReport;

    protected override async Task HandleAsync(GenerateReportPayload payload, CancellationToken ct)
    {
        await reportRepository.UpdateToProcessingAsync(payload.ReportId, ct);

        try
        {
            var generator = generators.FirstOrDefault(g => g.ReportTypeKey == payload.ReportTypeKey)
                ?? throw new InvalidOperationException($"No generator registered for report type '{payload.ReportTypeKey}'.");

            var parameters = new ReportParameters(
                DateOnly.Parse(payload.DateFrom),
                DateOnly.Parse(payload.DateTo));

            var excelBytes = await generator.GenerateAsync(payload.UserId, payload.WorkspaceId, payload.Language, parameters, ct);

            var fileKey = storageUtility.BuildFileKey(FolderPaths.Reports, $"{payload.ReportId}.xlsx");
            using var ms = new MemoryStream(excelBytes);
            await fileService.UploadAsync(
                ms,
                fileKey,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ct);

            await reportRepository.CompleteAsync(payload.ReportId, fileKey, excelBytes.Length, ct);

            // Lookup report type name for the email
            var types = await reportRepository.GetTypesAsync(ct);
            var type  = types.FirstOrDefault(t => t.Key == payload.ReportTypeKey);

            await notificationPublisher.PublishAsync(
                NotificationCodes.ReportReady,
                payload.UserId,
                parameters: new Dictionary<string, string>
                {
                    { "ReportType", payload.Language == "ar" ? (type?.NameAr ?? payload.ReportTypeKey) : (type?.NameEn ?? payload.ReportTypeKey) }
                },
                payload: new { reportId = payload.ReportId },
                ct: ct);

            await backgroundJobService.EnqueueAsync(
                JobTypes.ReportCompletedEmail,
                new ReportCompletedEmailPayload(
                    To:               payload.UserEmail,
                    Language:         payload.Language,
                    UserDisplayName:  payload.UserDisplayName,
                    ReportTypeNameEn: type?.NameEn ?? payload.ReportTypeKey,
                    ReportTypeNameAr: type?.NameAr ?? payload.ReportTypeKey,
                    DateFrom:         payload.DateFrom,
                    DateTo:           payload.DateTo,
                    GeneratedOn:      DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC"),
                priority: 2,
                ct: ct);
        }
        catch (Exception ex)
        {
            var message = ex.Message.Length > 1000
                ? ex.Message[..1000]
                : ex.Message;
            await reportRepository.FailAsync(payload.ReportId, message, ct);

            try
            {
                await notificationPublisher.PublishAsync(NotificationCodes.ReportFailed, payload.UserId, ct: ct);
            }
            catch { /* notification failure must not mask the original exception */ }

            throw;
        }
    }
}
