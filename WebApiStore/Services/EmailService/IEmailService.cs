using WebApiStore.DTOs.Email;

namespace WebApiStore.Services.EmailService
{
    public interface IEmailService
    {
        void SendEmail(EmailDTO request);
    }
}
