using Application.Common.Constants;
using Application.Features.Receipt.Jobs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Infrastructure.Jobs;
using Microsoft.Extensions.Logging;
using Shared.Enums.System;

namespace Infrastructure.Jobs.Handlers;

internal sealed class ProcessReceiptOcrHandler(
    IReceiptRepository         receiptRepository,
    IOcrProvider               ocrProvider,
    IFileService               fileService,
    IStorageUtility            storageUtility,
    ILogger<ProcessReceiptOcrHandler> logger) : JobHandlerBase<ProcessReceiptOcrPayload>
{
    public override string JobType => JobTypes.ProcessReceiptOcr;

    protected override async Task HandleAsync(ProcessReceiptOcrPayload payload, CancellationToken ct)
    {
        // Mark as Processing
        await receiptRepository.SetProcessingStatusAsync(payload.ReceiptId, 2, ct);

        if (!ocrProvider.CanProcess(payload.FileExtension, payload.ContentType))
        {
            await receiptRepository.SetProcessingStatusAsync(payload.ReceiptId, 5, ct);  // Skipped
            return;
        }

        var fileKey = storageUtility.BuildFileKey(FolderPaths.Receipts, payload.StoredFileName);

        try
        {
            await using var stream = await fileService.DownloadAsync(fileKey, ct);
            var result = await ocrProvider.ExtractAsync(stream, payload.FileExtension, ct);

            if (result is null)
            {
                // Provider returned no result (stub / no engine installed)
                await receiptRepository.SetProcessingStatusAsync(payload.ReceiptId, 5, ct);  // Skipped
                return;
            }

            await receiptRepository.SaveOcrResultAsync(
                receiptId:    payload.ReceiptId,
                rawText:      result.RawText,
                merchantName: result.MerchantName,
                totalAmount:  result.TotalAmount,
                receiptDate:  result.ReceiptDate,
                confidence:   result.Confidence,
                providerName: result.ProviderName,
                errorMessage: null,
                isSuccess:    true,
                ct:           ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OCR processing failed for receipt {ReceiptId}", payload.ReceiptId);

            await receiptRepository.SaveOcrResultAsync(
                receiptId:    payload.ReceiptId,
                rawText:      null,
                merchantName: null,
                totalAmount:  null,
                receiptDate:  null,
                confidence:   null,
                providerName: ocrProvider.ProviderName,
                errorMessage: ex.Message,
                isSuccess:    false,
                ct:           ct);
        }
    }
}
