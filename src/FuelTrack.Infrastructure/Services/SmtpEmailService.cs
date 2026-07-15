using FuelTrack.Application.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FuelTrack.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string frontendBaseUrl)
    {
        var subject = "FuelTrack — إعادة تعيين كلمة المرور";
        var resetUrl = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";
        var body = BuildPasswordResetHtml(userName, resetUrl);
        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendWeeklyReportAsync(string toEmail, string userName, WeeklyReportEmailData data)
    {
        var subject = $"FuelTrack — التقرير الأسبوعي ({data.ReportPeriod})";
        var body = BuildWeeklyReportHtml(userName, data);
        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendFuelStockAlertAsync(string toEmail, string userName, FuelStockAlertEmailData data)
    {
        var subject = $"FuelTrack — تنبيح مستوى الوقود ({data.TankName})";
        var body = BuildFuelStockAlertHtml(userName, data);
        return await SendEmailAsync(toEmail, subject, body);
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            _logger.LogWarning("[EMAIL] SMTP not configured — skipping email to {Email}", toEmail);
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.From));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = StripHtml(htmlBody)
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            var secureOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Host, _settings.Port, secureOptions);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
                await client.AuthenticateAsync(_settings.Username, _settings.Password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("[EMAIL] Sent to {Email}: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Failed to send to {Email}: {Subject}", toEmail, subject);
            return false;
        }
    }

    private static string BuildPasswordResetHtml(string userName, string resetUrl)
    {
        return $"""
        <!DOCTYPE html>
        <html dir="rtl" lang="ar">
        <head><meta charset="utf-8" /></head>
        <body style="font-family: Tahoma, Arial, sans-serif; background: #f4f6f8; margin: 0; padding: 20px;">
          <div style="max-width: 520px; margin: auto; background: #fff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,.08); overflow: hidden;">
            <div style="background: #1a237e; color: #fff; padding: 24px; text-align: center;">
              <h1 style="margin: 0; font-size: 22px;">FuelTrack</h1>
              <p style="margin: 6px 0 0; opacity: .85; font-size: 14px;">إعادة تعيين كلمة المرور</p>
            </div>
            <div style="padding: 32px 24px; text-align: center;">
              <p style="font-size: 15px; color: #333;">مرحباً <strong>{userName}</strong>،</p>
              <p style="font-size: 14px; color: #555; line-height: 1.7;">
                تلقّينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك.<br/>
                اضغط الزر أدناه لإنشاء كلمة مرور جديدة:
              </p>
              <a href="{resetUrl}" style="display: inline-block; margin: 20px 0; padding: 12px 36px; background: #1a237e; color: #fff; text-decoration: none; border-radius: 6px; font-size: 15px; font-weight: bold;">
                إعادة تعيين كلمة المرور
              </a>
              <p style="font-size: 12px; color: #999; margin-top: 24px;">
                هذا الرابط صالح لمدة ساعة واحدة فقط.<br/>
                إذا لم تطلب إعادة التعيين، تجاهل هذه الرسالة.
              </p>
            </div>
            <div style="background: #f9f9f9; padding: 14px; text-align: center; font-size: 11px; color: #aaa;">
              © {DateTime.UtcNow.Year} FuelTrack — نظام إدارة المحطات والوقود
            </div>
          </div>
        </body>
        </html>
        """;
    }

    private static string BuildWeeklyReportHtml(string userName, WeeklyReportEmailData data)
    {
        return $"""
        <!DOCTYPE html>
        <html dir="rtl" lang="ar">
        <head><meta charset="utf-8" /></head>
        <body style="font-family: Tahoma, Arial, sans-serif; background: #f4f6f8; margin: 0; padding: 20px;">
          <div style="max-width: 560px; margin: auto; background: #fff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,.08); overflow: hidden;">
            <div style="background: #1a237e; color: #fff; padding: 24px; text-align: center;">
              <h1 style="margin: 0; font-size: 22px;">FuelTrack</h1>
              <p style="margin: 6px 0 0; opacity: .85; font-size: 14px;">التقرير الأسبوعي — {data.ReportPeriod}</p>
            </div>
            <div style="padding: 32px 24px;">
              <p style="font-size: 15px; color: #333; text-align: center;">مرحباً <strong>{userName}</strong>،</p>
              <p style="font-size: 14px; color: #555; text-align: center; margin-bottom: 24px;">ملخص أداء الأسبوع:</p>
              <table style="width: 100%; border-collapse: collapse; font-size: 14px;">
                <tr style="background: #f5f7fa;">
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; font-weight: bold;">الفرع</td>
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; text-align: left;">{data.TotalBranches}</td>
                </tr>
                <tr>
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; font-weight: bold;">المحطات</td>
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; text-align: left;">{data.TotalStations}</td>
                </tr>
                <tr style="background: #f5f7fa;">
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; font-weight: bold;">المحطات الفرعية</td>
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; text-align: left;">{data.TotalSubStations}</td>
                </tr>
                <tr>
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; font-weight: bold;">المولدات</td>
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; text-align: left;">{data.TotalGenerators}</td>
                </tr>
                <tr style="background: #f5f7fa;">
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; font-weight: bold;">خزانات الوقود</td>
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; text-align: left;">{data.TotalFuelTanks}</td>
                </tr>
                <tr>
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; font-weight: bold;">إجمالي استهلاك الوقود (لتر)</td>
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; text-align: left;">{data.TotalFuelConsumedLiters:N0}</td>
                </tr>
                <tr style="background: #f5f7fa;">
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; font-weight: bold;">إجمالي الطاقة المُنتجة (kWh)</td>
                  <td style="padding: 10px 14px; border-bottom: 1px solid #eee; text-align: left;">{data.TotalEnergyProducedKWh:N0}</td>
                </tr>
                <tr>
                  <td style="padding: 10px 14px; font-weight: bold;">الكفاءة العامة</td>
                  <td style="padding: 10px 14px; text-align: left; color: {(data.OverallEfficiencyPercent >= 80 ? "#2e7d32" : "#c62828")}; font-weight: bold;">{data.OverallEfficiencyPercent:N1}%</td>
                </tr>
              </table>
              <p style="font-size: 12px; color: #999; text-align: center; margin-top: 24px;">
                تم إرسال هذا التقرير تلقائياً كل أسبوع.
              </p>
            </div>
            <div style="background: #f9f9f9; padding: 14px; text-align: center; font-size: 11px; color: #aaa;">
              © {DateTime.UtcNow.Year} FuelTrack — نظام إدارة المحطات والوقود
            </div>
          </div>
        </body>
        </html>
        """;
    }

    private static string BuildFuelStockAlertHtml(string userName, FuelStockAlertEmailData data)
    {
        var levelColor = data.Percentage <= 20 ? "#c62828" : data.Percentage <= 40 ? "#e65100" : "#f9a825";
        return $"""
        <!DOCTYPE html>
        <html dir="rtl" lang="ar">
        <head><meta charset="utf-8" /></head>
        <body style="font-family: Tahoma, Arial, sans-serif; background: #f4f6f8; margin: 0; padding: 20px;">
          <div style="max-width: 520px; margin: auto; background: #fff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,.08); overflow: hidden;">
            <div style="background: {levelColor}; color: #fff; padding: 24px; text-align: center;">
              <h1 style="margin: 0; font-size: 22px;">FuelTrack</h1>
              <p style="margin: 6px 0 0; opacity: .9; font-size: 14px;">تنبيح — مستوى الوقود منخفض</p>
            </div>
            <div style="padding: 32px 24px; text-align: center;">
              <p style="font-size: 15px; color: #333;">مرحباً <strong>{userName}</strong>،</p>
              <p style="font-size: 14px; color: #555; line-height: 1.7;">
                مستوى الوقود في الخزان <strong>{data.TankName}</strong> ({data.FuelType}) منخفض.
              </p>
              <div style="margin: 20px auto; background: #f0f0f0; border-radius: 20px; height: 24px; max-width: 360px; overflow: hidden;">
                <div style="width: {Math.Min(data.Percentage, 100)}%; height: 100%; background: {levelColor}; border-radius: 20px;"></div>
              </div>
              <p style="font-size: 16px; font-weight: bold; color: {levelColor};">{data.Percentage}%</p>
              <p style="font-size: 13px; color: #666;">{data.CurrentLevelLiters:N0} لتر من أصل {data.CapacityLiters:N0} لتر</p>
            </div>
            <div style="background: #f9f9f9; padding: 14px; text-align: center; font-size: 11px; color: #aaa;">
              © {DateTime.UtcNow.Year} FuelTrack — نظام إدارة المحطات والوقود
            </div>
          </div>
        </body>
        </html>
        """;
    }

    private static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
    }
}
