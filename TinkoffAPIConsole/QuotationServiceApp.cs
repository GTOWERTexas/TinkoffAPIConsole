using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using TinkoffAPIConsole.ModelAdditional;
using System;
using Grpc.Core;
using System.Drawing;
using System.Threading;
//using System.Windows.Forms;

namespace TinkoffAPIConsole
{
    internal class QuotationServiceApp
    {
        private readonly InvestApiClient investApiClient;
        private CancellationTokenSource cancellation;
        public QuotationServiceApp(InvestApiClient investApiClient)
        {
           
            this.investApiClient = investApiClient;
            
        }

        public string GetMarketDataHistory()
        {

            InstrumentServiceApp.GetFindInstrument(investApiClient);
            Console.WriteLine("Введите Figi инструмента:");
            string figi = Console.ReadLine();
            var candlesResponse = investApiClient.MarketData.GetCandles(
                new GetCandlesRequest
                {
                    Figi = figi,
                    From = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                    To = Timestamp.FromDateTime(DateTime.UtcNow),
                    Interval = CandleInterval.Hour
                }) ;
            var orderBookResponse = investApiClient.MarketData.GetOrderBook(new GetOrderBookRequest
            {
                Figi = figi,
                Depth = 20
            }) ;

            var lastPricesResponse = investApiClient.MarketData.GetLastPrices(new GetLastPricesRequest { Figi = {figi}});
            var lastTradesResponse = investApiClient.MarketData.GetLastTrades(new GetLastTradesRequest
            {
                Figi = figi,
                From = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-10)),
                To = Timestamp.FromDateTime(DateTime.UtcNow)    
            }) ;
            var closePricesResponse = investApiClient.MarketData.GetClosePrices(new GetClosePricesRequest
            {
                Instruments = {
                    new InstrumentClosePriceRequest {
                        InstrumentId = figi
                    } }
            }) ;
            return new MarketDataHistoryFormatter(candlesResponse, orderBookResponse, lastPricesResponse, lastTradesResponse, closePricesResponse).Format();
        }

        private class MarketDataHistoryFormatter
        {
            private readonly GetCandlesResponse candlesResponse;
            private readonly GetOrderBookResponse orderBookResponse;
            private readonly GetLastPricesResponse lastPricesResponse;
            private readonly GetLastTradesResponse lastTradesResponse;
            private readonly GetClosePricesResponse closePricesResponse;
            public MarketDataHistoryFormatter(GetCandlesResponse candlesResponse, GetOrderBookResponse orderBookResponse, 
                GetLastPricesResponse lastPricesResponse, GetLastTradesResponse lastTradesResponse, GetClosePricesResponse closePricesResponse)
            {
                this.candlesResponse = candlesResponse;
                this.orderBookResponse = orderBookResponse;
                this.lastPricesResponse = lastPricesResponse;
                this.closePricesResponse = closePricesResponse;
                this.lastTradesResponse = lastTradesResponse;
            }
            public string Format()
            {
               

                var sb = new StringBuilder();

                //
                Console.WriteLine(" Котировки за месяц(1 свеча - 1 день)/1 свеча - 1 час за неделю:");
                string canjs = JsonConvert.SerializeObject(candlesResponse);
                CandlesList? candles = JsonConvert.DeserializeObject<CandlesList>(canjs);
                int count = 1;
                foreach (var item in candles.candles)
                {
                    if (item?.Open.Units < item?.Close.Units || ((item?.Open.Units == item?.Close.Units) && (item?.Open.Nano < item?.Close.Nano)))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    Console.WriteLine($"{count}) date: {item.Time} | open: {item.Open.Units},{item.Open.Nano.ToString().TrimEnd('0') + '0',-3} | close: {item.Close.Units}," +
                    $"{item.Open.Nano.ToString().TrimEnd('0') + '0',-3} | low: {item.Low.Units},{item.Open.Nano.ToString().TrimEnd('0') + '0',-3} | high: {item.High.Units},{item.Open.Nano.ToString().TrimEnd('0') + '0',-3} | volume in lots: {item.Volume,-3}");
                    //sb.AppendFormat($"open: { item.Open.Units},{ item.Open.Nano.ToString().TrimEnd('0') + '0',-3} | close: { item.Close.Units}")
                    //    .Append($"{item.Close.Nano.ToString().TrimEnd('0') + '0',-3} | low: {item.Low.Units},{item.Open.Nano.ToString().TrimEnd('0') + '0',-3}")
                    //    .Append($"| high: {item.High.Units},{item.Open.Nano.ToString().TrimEnd('0') + '0',-3} | volume in lots: {item.Volume}").AppendLine();  
                    count++;
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();
                //
                Console.WriteLine($"Последняя цена: ");
                string lspjs = JsonConvert.SerializeObject(lastPricesResponse);
                LastPrices? lastPrices = JsonConvert.DeserializeObject<LastPrices>(lspjs);
                foreach (var item in lastPrices.lastPrices)
                {
                    Console.WriteLine($"{item.Price.Units},{ item.Price.Nano.ToString().TrimEnd('0') + '0'} time: {item.Time.Seconds}");
                }
                Console.WriteLine();
                //
                Console.WriteLine($"Цена закрытия: ");
                string clpjs = JsonConvert.SerializeObject(closePricesResponse);
                ClosePricesList? closePricesList = JsonConvert.DeserializeObject<ClosePricesList>(clpjs);
                foreach (var item in closePricesList.closeprices)
                {
                    Console.WriteLine($"{item.Price.Units},{item.Price.Nano.ToString().TrimEnd('0') + '0'}");
                }
                Console.WriteLine();
                //
                sb.AppendLine().AppendFormat($"Цена закрытия: ").AppendLine();
                string ltpjs = JsonConvert.SerializeObject(lastTradesResponse);
                TradesList? trades = JsonConvert.DeserializeObject<TradesList>(ltpjs);
                Console.WriteLine("Time\t\t\t\t\tPrice\t\tDirection\tLots\tSum\n============================\t\t======\t\t=========\t==\t===");
                foreach (var item in trades.trades)
                {
                  
                    Console.Write($"{item.Time,-3}\t\t{item.Price.Units},{item.Price.Nano.ToString().TrimEnd('0') + '0'}\t\t");
                    if (item.Direction == TradeDirection.Buy)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (item.Direction == TradeDirection.Sell)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.Write($"{(item.Direction == TradeDirection.Buy ? "Покупка" : "Продажа")}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"\t\t{item.Quantity, -3}\t{((item.Price.Units+item.Price.Nano/10000000000)*item.Quantity)}");
                    Console.WriteLine();

                }
               sb.AppendLine("Сервис с историей котировок завершил работу");
                return sb.ToString();

            }
        }


        public async Task GetMarketDataStream()
        {
           CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken cancellationToken = cts.Token;

            var stream = investApiClient.MarketDataStream.MarketDataStream();
           
            InstrumentServiceApp.GetFindInstrument(investApiClient);
            Console.WriteLine("Введите Figi инструмента:");
            string figi = Console.ReadLine();
            Console.WriteLine("Выберите вариант котировок:");
            Console.WriteLine("1 - поток обезличенных сделок\n2 - свечи\n3 - последняя цена");
            string variableQuat = Console.ReadLine();
           
              switch (variableQuat)
            {
                case "1":
                    Console.WriteLine("Time\t\t\t\t\tPrice\t\tDirection\tLots\tSum\n============================\t\t======\t\t=========\t==\t===");
                    await stream.RequestStream.WriteAsync(
                      new MarketDataRequest
                      {

                          SubscribeTradesRequest = new SubscribeTradesRequest
                          {
                              Instruments =
                              {
                                    new TradeInstrument
                                    {
                                        Figi = figi

                                    }
                              },
                              SubscriptionAction = SubscriptionAction.Subscribe

                          }
                      }
              );
                    break;
                case "2":
                    await stream.RequestStream.WriteAsync(new MarketDataRequest
                    {
                        SubscribeCandlesRequest = new SubscribeCandlesRequest
                        {
                            Instruments =
                            {
                                new CandleInstrument
                                {
                                    Figi = figi,
                                    Interval = SubscriptionInterval.OneMinute
                                }
                            },
                            SubscriptionAction = SubscriptionAction.Subscribe
                        }
                    });

                    break;
                case "3":
                    await stream.RequestStream.WriteAsync(new MarketDataRequest
                    {
                        SubscribeLastPriceRequest = new SubscribeLastPriceRequest
                        {
                            Instruments =
                                {
                                    new LastPriceInstrument
                                    {
                                        Figi = figi
                                    }
                                },
                            SubscriptionAction = SubscriptionAction.Subscribe
                        }
                    });

                    break;
                case "4":

                    break;
                case "5":
                default:
                    break;
            }
           
            try
            {
               
                await foreach (var item in stream.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    Console.CancelKeyPress += (sender, eventArgs) =>
                    {
                        eventArgs.Cancel = true;
                        cts.Cancel();
                    };

                    switch (variableQuat)
                    {
                        
                        case "1":
                            if (item.Trade != null)
                            {


                                Console.Write(($"{item.Trade.Time,-3}\t{DateTime.UtcNow,-3}\t{item.Trade.Price.Units+(decimal)item.Trade.Price.Nano/1000000000}\t\t"));
                                if (item.Trade.Direction == TradeDirection.Buy)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                }
                                else if (item.Trade.Direction == TradeDirection.Sell)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.White;
                                }
                                Console.Write($"{(item.Trade.Direction == TradeDirection.Buy ? "Покупка" : "Продажа")}");

                                Console.ForegroundColor = ConsoleColor.White;
                                Console.Write($"\t\t{item.Trade.Quantity,-3}\t{(item.Trade.Price.Units + (decimal)item.Trade.Price.Nano / 1000000000) * item.Trade.Quantity}");
                                Console.WriteLine();

                            }
                            else { Console.WriteLine("Загрузка"); }
                            break;
                        case "2":
                            if (item.Candle != null)
                            {
                                if (item.Candle?.Open.Units < item.Candle?.Close.Units || ((item.Candle?.Open.Units == item.Candle?.Close.Units) && (item.Candle?.Open.Nano < item.Candle?.Close.Nano)))
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                }
                                Console.WriteLine($"Open: {item.Candle?.Open.Units},{item.Candle?.Open.Nano.ToString().TrimEnd('0')} rub | " +
                                    $"Close: {item.Candle?.Close.Units},{item.Candle?.Close.Nano.ToString().TrimEnd('0')} rub");
                            }
                            else
                            {
                                Console.WriteLine("Загрузка");
                            }

                            break;
                        case "3":
                            if (item.LastPrice != null)
                            {
                                Console.WriteLine($"price: {(decimal)item.LastPrice.Price.Units+(decimal)item.LastPrice.Price.Nano/1000000000}");
                            }
                            else
                            {
                                Console.WriteLine($"Загрузка/Ошибка: неправильно набран номер");
                            }
                           
                            break;
                        case "4":

                            break;
                        case "5":

                            break;
                        default:
                            break;
                    }
                }
            }
            catch (RpcException e) when (e.Status.StatusCode == StatusCode.Cancelled)
            {
                Console.WriteLine("Streaming was cancelled from the client!");
            }
            Console.ForegroundColor = ConsoleColor.White;

            
        }
        
    }
   
}
