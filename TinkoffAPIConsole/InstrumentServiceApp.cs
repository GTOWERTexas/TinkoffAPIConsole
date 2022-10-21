using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi;
using Newtonsoft.Json;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using TinkoffAPIConsole.ModelAdditional;

namespace TinkoffAPIConsole
{
    internal class InstrumentServiceApp
    {
        //private readonly InstrumentsService.InstrumentsServiceClient investClient;

        //public InstrumentServiceApp(InstrumentsService.InstrumentsServiceClient investClient)
        //{
        //    this.investClient = investClient;
        //}
        private readonly InvestApiClient investClient;

        public InstrumentServiceApp(InvestApiClient investClient)
        {
            this.investClient = investClient;
        }

        public async Task<string> GetInstrumentsDescription()
        {
            var shares = await investClient.Instruments.SharesAsync();
            var bonds = await investClient.Instruments.BondsAsync();
            var etfs = await investClient.Instruments.EtfsAsync();

            var dividends = new List<GetDividendsResponse>(3);
            foreach (var share in shares.Instruments.Take(dividends.Capacity))
            {
                var dividendsResponse = await investClient.Instruments.GetDividendsAsync(new GetDividendsRequest
                {
                    Figi = share.Figi,
                    From = share.IpoDate,
                    To = Timestamp.FromDateTime(DateTime.UtcNow)
                }
                    );
                dividends.Add(dividendsResponse);
            }

            var accuredInterests = new List<GetAccruedInterestsResponse>(3);
            foreach (var bond in bonds.Instruments.Take(accuredInterests.Capacity))
            {
                var accuredInterestResponse = await investClient.Instruments.GetAccruedInterestsAsync(new GetAccruedInterestsRequest
                {
                    Figi = bond.Figi,
                    From = bond.PlacementDate,
                    To = Timestamp.FromDateTime(DateTime.UtcNow)
                }
                    );
                accuredInterests.Add(accuredInterestResponse);
            }

            var traidingSheduleResponse = await investClient.Instruments.TradingSchedulesAsync(new TradingSchedulesRequest
            {
                Exchange = "MOEX",
                From = Timestamp.FromDateTime(DateTime.UtcNow.Date.AddDays(1)),
                To = Timestamp.FromDateTime(DateTime.UtcNow.Date.AddDays(3))
            }

            ) ;
           

            return new InstrumentsFormatter(shares.Instruments, accuredInterests, etfs.Instruments, bonds.Instruments, dividends, traidingSheduleResponse).Format();

        }
        private class InstrumentsFormatter{

            private readonly IReadOnlyList<Share> shares;
            private readonly IReadOnlyList<GetAccruedInterestsResponse> accruedInterestsResponses;
            private readonly IReadOnlyList<Etf> etfs;
            private readonly IReadOnlyList<Bond> bonds;
            private readonly IReadOnlyList<GetDividendsResponse> dividends;
            private readonly TradingSchedulesResponse tradingSchedulesResponse;
            public InstrumentsFormatter(IReadOnlyList<Share> share,
            IReadOnlyList<GetAccruedInterestsResponse> accruedInterestsResponses, IReadOnlyList<Etf> etfs, IReadOnlyList<Bond> bonds,  IReadOnlyList<GetDividendsResponse> getDividendsResponses,TradingSchedulesResponse tradingSchedulesResponse)
            {
                shares = share; 
                this.accruedInterestsResponses = accruedInterestsResponses;
                this.tradingSchedulesResponse = tradingSchedulesResponse;
                this.etfs = etfs;
                this.bonds = bonds;
                this.dividends = getDividendsResponses;
            }
            public string Format()
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (var shedule in tradingSchedulesResponse.Exchanges)
                {
                    stringBuilder.AppendFormat($"Trading shedule for exchange: {shedule.Exchange}").AppendLine();
                    foreach (var tradingDay in shedule.Days)
                    {
                        stringBuilder.AppendFormat("- {0} {1:working;0;non-working} {2} {3}", tradingDay.Date,
                             tradingDay.IsTradingDay.GetHashCode(), tradingDay.StartTime, tradingDay.EndTime)
                         .AppendLine();
                    }
                }
                stringBuilder.AppendLine("...");
                stringBuilder.AppendFormat($"Loaded {shares.Count} shares").AppendLine();
                for (int i = 0; i < 10; i++)
                {
                    var share = shares[i];
                    stringBuilder.AppendFormat($"*[{share.Figi}], {share.Name}, {share.Ticker}").AppendLine();

                    if (i<dividends.Count)
                    {
                        var dividendCount = Math.Min(10, dividends[i].Dividends.Count);
                        if (dividendCount == 0)
                        {
                            continue;
                        }
                        stringBuilder.AppendFormat($" Dividends: ");
                        for (int j = 0; j < dividendCount; j++)
                        {
                            var dividend = dividends[i].Dividends[j];
                            stringBuilder.AppendFormat($" - {(decimal)dividend.DividendNet} {dividend.DividendNet.Currency}  {dividend.DividendType} {dividend.DeclaredDate}").AppendLine();
                        }
                        
                    }
                }
                stringBuilder.AppendLine("...").AppendLine();

                stringBuilder.AppendFormat($"Loaded {etfs.Count} etfs:").AppendLine();
                for (int i = 0; i < 10; i++)
                {
                    var etf = etfs[i];
                    stringBuilder.AppendFormat($"*[{etf.Figi}], {etf.Name}, {etf.Ticker}").AppendLine();
                }
                stringBuilder.AppendLine("...");
                stringBuilder.AppendFormat($"Loaded {bonds.Count} bonds").AppendLine();
                for (int i = 0; i < 10; i++)
                {
                    var bond = bonds[i];
                    stringBuilder.AppendFormat($"*[{bond.Figi}], {bond.Name}, {bond.Ticker}").AppendLine();
                    if (i < accruedInterestsResponses.Count)
                    {
                        var accuredInterestsCount = Math.Min(10, accruedInterestsResponses[i].AccruedInterests.Count);
                       
                        for (int j = 0; j < accuredInterestsCount; j++)
                        {
                            var accured = accruedInterestsResponses[i].AccruedInterests[j];
                            stringBuilder.AppendFormat($" - {(decimal)accured.Nominal} {accured.Value.Units},{accured.Value.Nano.ToString().TrimEnd('0')}, {accured.Date}").AppendLine();
                        }
                    }
                }
                stringBuilder.AppendLine("...");

                return stringBuilder.ToString();
            }
            
                

        }
        
        
        public  void GetInfoBaseCurrencies()
        {
            var currencies = investClient.Instruments.Currencies(new InstrumentsRequest { InstrumentStatus = InstrumentStatus.Base});
          
          Console.Clear();

            Console.WriteLine("\tCurrencies");
            Console.WriteLine("Tiker(iso):\t\tFigi:\t\tName:\t\t\tShortEnabledFlag:\t Price in RUB:");
            Console.WriteLine("-----------\t\t------------\t-----\t\t\t----------------\t -----------");
            int count = 3;
            foreach (var cur in currencies.Instruments)
            {
                    var lastprice = investClient.MarketData.GetLastPrices(new GetLastPricesRequest
                    {
                        Figi =
                        {
                           cur.Figi
                        }
                    });
             
                if (cur.IsoCurrencyName == "rub" || cur.IsoCurrencyName == "eur" || cur.IsoCurrencyName == "usd")
                {
                    Console.Write($"{cur.Ticker}, {cur.IsoCurrencyName}\t{cur.Figi}\t{cur.Name}");
                    Console.SetCursorPosition(64, count);
                    Console.Write($"{cur.ShortEnabledFlag}");
                    Console.SetCursorPosition(89, count);

                    if (cur.IsoCurrencyName != "rub")
                    {
                        string json = JsonConvert.SerializeObject(lastprice);
                        LastPrices? lastPrices = JsonConvert.DeserializeObject<LastPrices>(json);
                        foreach (var item in lastPrices.lastPrices)
                        {
                            Console.Write($"{item.Price.Units},{item.Price.Nano.ToString().TrimEnd('0') + '0'} rub");
                        }
                    }
                    else
                    {
                        Console.Write("1");
                    }

                    Console.WriteLine();

                }
               
                else
                {
                    Console.WriteLine($"{cur.Ticker}, {cur.IsoCurrencyName}\t\t{cur.Figi}\t{cur.Name}");
                    Console.SetCursorPosition(64, count);
                    Console.Write($"{cur.ShortEnabledFlag}");
                    Console.SetCursorPosition(89, count);

                    if (cur.IsoCurrencyName != "zar" && cur.IsoCurrencyName != "aed"&& cur.IsoCurrencyName != "xag" )
                    {
                        string json = JsonConvert.SerializeObject(lastprice);
                        LastPrices? lastPrices = JsonConvert.DeserializeObject<LastPrices>(json);

                        foreach (var item in lastPrices.lastPrices)
                        {
                            Console.Write($"{item.Price.Units},{item.Price.Nano.ToString().TrimEnd('0') + '0'} rub");
                        }
                    }
                    else
                    {
                        Console.Write("limit request");
                    }
                    

                    Console.WriteLine();
                }
                count++;
            }
        }
        public static void GetFindInstrument(InvestApiClient investClient)
        {
            Console.WriteLine("\tЭто поисковик инструментов\n Введи название инструмента или любой из ее индентификаторов\n" +
                " Затем получаешь его Figi => копирую его и вставляй, когда командная строка это потребует\n И ты получишь нужные котировки, если воспользуешься сервисом QuatationServiceApp ");
            string query = Console.ReadLine();
            var findInstrumentResponse = investClient.Instruments.FindInstrument(new FindInstrumentRequest { Query = query });
            string fijs = JsonConvert.SerializeObject(findInstrumentResponse);
            InstrumentsList? instrumentsShort = JsonConvert.DeserializeObject<InstrumentsList>(fijs);
            foreach (var item in instrumentsShort.instruments)
            {
                Console.WriteLine($"Название инструмениа: {item.Name,-1} {item.Ticker,-8} | Figi инструмента: {item.Figi,-8} | Type: {item.InstrumentType,-8}");
                
            }
        }


    }
}
