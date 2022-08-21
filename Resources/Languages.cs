using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Resources
{
    public static class Languages
    {
        public static Dictionary<string, LanguageData> Language = new Dictionary<string, LanguageData>
        {
            {
                "en", new LanguageData
                {
                    Culture = "en",
                    StartOrPauseEvent = "Start/Pause Event",
                    StopEvent = "Stop Event",
                    Join = "Join",
                    CreatedAt = "Created At",
                    EndedAt = "Ended At",
                    Event = "Event",
                    Users = "Users",
                    PeakTime = "Peak Time",
                    Total = "Total",
                    TotalTime = "Total Time",
                    Amount = "Amount",
                    EventTax = "Event Tax",
                    BuyerTax = "Buyer Tax",
                    PercentagePlaceHolder = "Percentage",
                    Expire = "Expire",
                    License = "License",
                    LicenseNotFound = "License not found",
                    LicenseExpired = "License expired",
                    LicenseHasAlreadyBeenInstalled = "License has already been installed",
                    EventBalance = "Event Balance",
                    Balance = "Balance",
                    AmountPlaceHolder = "Amount",
                    AccountWithoutBalance = "Your account has no balance",
                    User = "User"
                }
            },
            {
                "pt", new LanguageData
                {
                    Culture = "pt",
                    StartOrPauseEvent = "Iniciar/Pausar Evento",
                    StopEvent = "Parar Evento",
                    Join = "Entrar",
                    CreatedAt = "Criado",
                    EndedAt = "Acabou",
                    Event = "Evento",
                    Users = "Usuários",
                    PeakTime = "Tempo Máximo",
                    Total = "Total",
                    TotalTime = "Tempo Total",
                    Amount = "Montante",
                    EventTax = "Taxa de Evento",
                    BuyerTax = "Taxa de Comprador",
                    PercentagePlaceHolder = "Porcentagem",
                    Expire = "Expira",
                    License = "Licença",
                    LicenseNotFound = "Licença não encontrada",
                    LicenseExpired = "Licença expirada",
                    LicenseHasAlreadyBeenInstalled = "Uma licença já foi instalada",
                    EventBalance = "Saldo do Evento",
                    Balance = "Saldo",
                    AmountPlaceHolder = "Qunaitdade",
                    AccountWithoutBalance = "Sua conta não possui um registro de saldo",
                    AccountAmountUpdated = "Saldo Atualizado",
                    User = "Usuário"
                }
            }
        };
    }

    public class LanguageData
    {
        public string Culture { get; set; }
        public string StartOrPauseEvent { get; set; }
        public string StopEvent { get; set; }
        public string Join { get; set; }
        public string CreatedAt { get; set; }
        public string EndedAt { get; set; }
        public string Event { get; set; }
        public string Users { get; set; }
        public string PeakTime { get; set; }
        public string Total { get; set; }
        public string TotalTime { get; set; }
        public string Amount { get; set; }
        public string EventTax { get; set; }
        public string BuyerTax { get; set; }
        public string PercentagePlaceHolder { get; set; }
        public string Expire { get; set; }
        public string License { get; set; }
        public string LicenseNotFound { get; set; }
        public string LicenseExpired { get; set; }
        public string LicenseHasAlreadyBeenInstalled { get; set; }
        public string EventBalance { get; set; }
        public string Balance { get; set; }
        public string AmountPlaceHolder { get; set; }
        public string AccountWithoutBalance { get; set; }
        public string AccountAmountUpdated { get; set; }
        public string User { get; set; }
    }
}
