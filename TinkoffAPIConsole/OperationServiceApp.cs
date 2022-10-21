using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace TinkoffAPIConsole
{
    internal class OperationServiceApp
    {
        private readonly InvestApiClient investApiClient;

        public OperationServiceApp(InvestApiClient _investApiClient)
        {
            investApiClient = _investApiClient;
        }

        public string GetOperationsDescriptions()
        {
            var accounts =  investApiClient.Users.GetAccounts();
            var accountId = accounts.Accounts.First().Id;

            var operations = investApiClient.Operations;
            var portfolio =  operations.GetPortfolio(new PortfolioRequest { AccountId = accountId});
            var positions =  operations.GetPositions(new PositionsRequest { AccountId = accountId });
            var withdrawLimits =  operations.GetWithdrawLimits(new WithdrawLimitsRequest { AccountId = accountId });

            var operationsResponse =  operations.GetOperations(new OperationsRequest
            {
                AccountId = accountId,
                From = Timestamp.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
                To = Timestamp.FromDateTime(DateTime.UtcNow)
            }) ;
           return  new OperationsFormatter(operationsResponse, portfolio, positions, withdrawLimits).Format();
        }
        private class OperationsFormatter
        {
            private readonly OperationsResponse operations;
            private readonly PortfolioResponse portfolio;
            private readonly PositionsResponse positions;
            private readonly WithdrawLimitsResponse withdrawLimits;

            public OperationsFormatter(OperationsResponse _operations, PortfolioResponse _portfolio, PositionsResponse _positions, WithdrawLimitsResponse _withdrawLimitsResponse)
            {
                operations = _operations;
                portfolio = _portfolio;
                positions = _positions;
                withdrawLimits = _withdrawLimitsResponse;
            }
            public string Format()
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Portfolio: ")
                    .AppendFormat($"-Currency {(decimal)portfolio.TotalAmountCurrencies} {portfolio.TotalAmountCurrencies.Currency}")
                    .AppendLine()
                    .AppendFormat($"-Shares {(decimal)portfolio.TotalAmountShares} {portfolio.TotalAmountShares.Currency}")
                    .AppendLine()
                    .AppendFormat($"-Bonds {(decimal)portfolio.TotalAmountBonds} {portfolio.TotalAmountBonds.Currency}")
                     .AppendLine()
                    .AppendFormat($"-ETF {(decimal)portfolio.TotalAmountEtf} {portfolio.TotalAmountEtf.Currency}")
                     .AppendLine()
                    .AppendFormat($"-Futures {(decimal)portfolio.TotalAmountFutures} {portfolio.TotalAmountFutures.Currency}" ).AppendLine()
                    .AppendFormat($"-Expected yield {portfolio.ExpectedYield.Units},{portfolio.ExpectedYield.Nano.ToString().TrimEnd('0') + '0' + " %"}" ).AppendLine();
                    
                if (withdrawLimits.Money.Any())
                {
                    stringBuilder.AppendLine().AppendLine("Withdraw limits:");
                    foreach (var value in withdrawLimits.Money)
                        stringBuilder.AppendFormat("- {0} {1}", (decimal)value, value.Currency)
                            .AppendLine();
                }

                if (positions.Securities.Any())
                {
                    stringBuilder.AppendLine().AppendLine("Positions:");
                    foreach (var security in positions.Securities)
                        stringBuilder.AppendFormat("- [{0}] {1} {2} {3}", security.Figi, security.Balance, security.Balance * portfolio.Positions.First(c=>c.Figi == security.Figi).CurrentPrice, security.InstrumentType)
                            .AppendLine();
                }

                if (operations.Operations.Any())
                {
                    stringBuilder.AppendLine().AppendLine("Operations:");
                    foreach (var operation in operations.Operations)
                        stringBuilder.AppendFormat("- [{0}] {1} {2} {3}", operation.Figi, operation.Date,
                                (decimal)operation.Payment, operation.Currency)
                            .AppendLine();
                }
                stringBuilder.AppendLine();
                stringBuilder.AppendLine().AppendLine("Portfolio positions");
                foreach (var position in portfolio.Positions)
                {
                    if (position.Quantity.Units>0)
                    {
                        stringBuilder.Append($" - {position.Figi} {position.Quantity.Units} * {position.CurrentPrice.Units + ((decimal)position.CurrentPrice.Nano / (decimal)1000000000)}");
                        stringBuilder.Append($" = {position.Quantity.Units * (position.CurrentPrice.Units + ((decimal)position.CurrentPrice.Nano / (decimal)1000000000))} {position.CurrentPrice.Currency} | {position.InstrumentType} ").AppendLine();

                    }
                    else
                    {
                        stringBuilder.Append($" - {position.Figi} {(decimal)position.Quantity.Nano/1000000000} * {position.CurrentPrice.Units + (decimal)position.CurrentPrice.Nano/ (decimal)1000000000}");
                        stringBuilder.Append($" = {(decimal)position.Quantity.Nano / 1000000000 * (position.CurrentPrice.Units + (decimal)position.CurrentPrice.Nano / 1000000000)}  {position.CurrentPrice.Currency} | {position.InstrumentType} ").AppendLine();
                    }
                }
                                return stringBuilder.ToString();
            }

        }
    }
}
