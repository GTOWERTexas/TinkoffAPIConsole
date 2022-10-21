using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi;

namespace TinkoffAPIConsole
{
    internal class UserServiceApp
    {
        private readonly InvestApiClient investApiClient; //UsersService.UsersServiceClient usersServiceClient;
        public UserServiceApp(InvestApiClient _investApiClient)
        {
            investApiClient = _investApiClient;
        }

        public string GetUserDescription()
        {
           var accountsResponse = investApiClient.Users.GetAccounts();
           var info = investApiClient.Users.GetInfo();
           var userTariff = investApiClient.Users.GetUserTariff();

            return new UserFormatter(accountsResponse.Accounts, info, userTariff).Format();
        }

        public class UserFormatter
        {
            private readonly IReadOnlyCollection<Account> accounts;
            private readonly GetInfoResponse info;
            private readonly GetUserTariffResponse userTariff;

            public UserFormatter(IReadOnlyCollection<Account> accounts, GetInfoResponse info, GetUserTariffResponse userTariff)
            {
                this.accounts = accounts;
                this.info = info;
                this.userTariff = userTariff;
            }

            public string Format()
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine().AppendLine("User: ").
                    Append("prem status: ").Append($"{(info.PremStatus == true ? "yes" : "no")}").AppendLine()
                    .Append("qual status: ").Append($"{(info.QualStatus == true ? "yes" : "no")}").AppendLine()
                    .Append($"tariff: {info.Tariff}").AppendLine();
                stringBuilder.AppendLine().AppendFormat($"Current account: {accounts.Count()}").AppendLine();
                foreach (var account in accounts)
                {
                    stringBuilder.AppendLine().Append($"accountId: {account.Id}").AppendLine()
                        .Append($"account Name: {account.Name}").AppendLine()
                        .Append($"account Status: {account.Status}").AppendLine()
                        .Append($"account Type: {(((int)account.Type) == 1 ? "Брокерский счет Тинькофф" : account.Type)}").AppendLine();
                }
                stringBuilder.AppendLine().AppendLine("Limits: ");

                foreach (var limits in userTariff.UnaryLimits)
                {
                    foreach (var method in limits.Methods)
                    {
                        stringBuilder.AppendFormat($"{limits.LimitPerMinute} Rps for method: {method}").AppendLine();
                    }
                }
                stringBuilder.AppendLine();
                foreach (var limits in userTariff.StreamLimits)
                {
                    foreach (var stream in limits.Streams)
                    {
                        stringBuilder.AppendFormat($"{limits.Limit} connection for stream: {stream}").AppendLine();
                    }
                }

                return stringBuilder.ToString();
            }

        }


    }
}
