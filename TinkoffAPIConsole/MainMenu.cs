using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi;

namespace TinkoffAPIConsole
{
    public class MainMenu
    {
        CancellationTokenSource source = null;
        public static async Task PrintMainMenu(InvestApiClient client)
        {
            bool canExist = true;
            string answer = null;

           
            while (canExist)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("==================================================================").AppendLine()
              .AppendLine("|  Добро пожаловать в Tinkoff InvestAPI console application    |")
              .AppendLine("|  Выберите сервис:                                            |").AppendLine("|                                                              |")
              .AppendLine("|   1 - UserService: Информация о  клиенте, его счета, статус, |")
              .AppendLine("|                    тарифы и лимиты                           |")
              .AppendLine("|   2 - OperationService: Операции по счету, открытые позиции, |")
              .AppendLine("|                    доступный остаток для вывода средств      |")
              .AppendLine("|   3 - InstrumentService: Информация о торговых инструментах, |")
              .AppendLine("|                    расписание торгов, дивиденды, купоны      |")
              .AppendLine("|   4 - QuatationService: Сервис получения биржевой информации |")
              .AppendLine("|                    (свечи, стаканы, торговые статусы)        |")
              .AppendLine("|   5 - StopOrdersService: Выставление и отмена заявок, работа |")
              .AppendLine("|                    со стоп-заявками                          |")
              .AppendLine("|   6 - OrdersService: Сервис торговых поручений               |")
              .AppendLine("==================================================================");
                Console.WriteLine(sb);
                Console.WriteLine("Ваш выбор: ");
                answer = Console.ReadLine();

                switch (answer)
                {
                    case "1":
                        {
                            UserServiceApp userService = new UserServiceApp(client);
                            Console.WriteLine(userService.GetUserDescription());
                        }
                        break;
                    case "2":
                        {
                            OperationServiceApp operationServiceApp = new OperationServiceApp(client);
                            Console.WriteLine(operationServiceApp.GetOperationsDescriptions());
                        }
                        break;
                    case "3":
                        {
                            InstrumentServiceApp instrumentServiceApp  = new InstrumentServiceApp(client);
                            Console.WriteLine(" 1 - Валютные инструменты\n 2 - Все инструменты");
                            string ins = Console.ReadLine();
                            switch (ins)
                            {
                                case "1":  instrumentServiceApp.GetInfoBaseCurrencies(); break;
                                case "2": Console.WriteLine(await instrumentServiceApp.GetInstrumentsDescription()); break;
                                default:
                                    Console.WriteLine("Введите 1 или 2");
                                    break;
                            }
                        }
                        break;
                    case "4":
                        {
                           QuotationServiceApp quotationServiceApp = new QuotationServiceApp(client);
                            Console.WriteLine(" 1 - Исторические котиовки\n 2 - Стриминговые(In real time) котировки");
                            string ins = Console.ReadLine();
                            switch (ins)
                            {
                                case "1": Console.WriteLine(quotationServiceApp.GetMarketDataHistory()); ; break;
                                case "2":
                                    
                                        await quotationServiceApp.GetMarketDataStream();
                                   
                                     break ;
                                default:
                                    Console.WriteLine("Введите 1 или 2");
                                    break;
                            }
                            
                        }
                        break;
                    case "5":
                        {
                            Console.WriteLine("Сервис еще не создан");
                        }
                        break;
                    case "6":
                        {
                            Console.WriteLine("Сервис еще не создан");
                        }
                        break;
                    case "q":
                    case "Q":
                    case "й":
                    case "Й":
                        {
                            canExist = false;
                        }
                        break;
                    default:
                        Console.WriteLine($"Ошибка: {answer} - варианта ответа не существует ERROR ERROR ERROR") ;
                        break;
                }

            }
        }
    }
}
