using System;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using ImapX;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace EmailConfirmation
{
    class Program
    {
        static void Main(string[] args)
        {
            //Если существует файл с настройками
            if (File.Exists("settings.json"))
            {
                //Считываем файл настроек
                string settings_file = File.ReadAllText("settings.json");

                //Создаем объект типа jobject
                JObject settings = JObject.Parse(settings_file);

                //Создадим объект класса ImapX
                var client = new ImapClient(settings["IMAP"].ToString(), Convert.ToInt32(settings["Port"]), true);
                
                //Объявляем регулярное выражение для поиска нужной ссылки
                var find_url = new Regex("style=\"background:#799905;\"><a href=\"(.*)\"> <span style=\"border-radius:2px;");

                //Проверим успешно ли подключились
                if (client.Connect())
                {
                    //Выводим информацию в консоль, что подключились
                    Console.WriteLine("Successfully connected");

                    //Пробуем войти в почтовый ящик
                    if (client.Login(settings["Mail"].ToString(), settings["Password"].ToString()))
                    {
                        //Пишем в консоль, что вошли успешно
                        Console.WriteLine("Login successfully");

                        //Теперь будем постоянно обновлять нашу почту - раз в N секунд
                        while (true)
                        {
                            //Выведем в консоль что пошла новая проверка
                            Console.WriteLine("{0} - Checking mailbox...", DateTime.Now);

                            //Пробежимся по всем непрочитанным письмам
                            foreach (var mess in client.Folders["INBOX"].Search("UNSEEN"))
                            {
                                //Если это сообщение от стима
                                if (mess.Subject == "Steam Trade Confirmation")
                                {
                                    //Пробуем найти ссылку
                                    try
                                    {
                                        //Выводим сообщение, что нашли новое письмо
                                        Console.WriteLine("Find new unred message from " + mess.From);

                                        //Найдем совпадения
                                        Match match = find_url.Match(mess.Body.Html.ToString());

                                        //Преобразуем ссылку в правильный формат
                                        string url = match.Groups[1].Value.Replace("&amp;", "&");

                                        //Если проверим нашло ли вообще ссылку
                                        if (String.IsNullOrEmpty(url))
                                        {
                                            //Отправим подтверждение
                                            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                                        }
                                    }
                                    catch(Exception e)
                                    {
                                        Console.Write("Url was not found");
                                    }

                                    //Отметим письмо как прочитанное
                                    mess.Seen = true;
                                }
                            }

                            //Поставим паузу на нужное количесво сепкунд
                            Thread.Sleep(Convert.ToInt32(settings["UpdateTime"]) * 1000);
                        }
                    }
                    else
                        Console.WriteLine("Bad login/password");
                }
                else
                    Console.WriteLine("Connection error");
            }
        }
    }
}
