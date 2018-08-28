using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Configuration;

namespace BIA_Technologies__Report_Generator_ 
{
    //[FIXME] Плохой стиль именования неймспейса. Не обязательно писать полное имя компании, а дочерние неймспейсы нужно отделять точками

    static public class CommandHandler
    {
        //[FIXME] То есть, консоль на ввод условной команды deactivate выдаст мне отчет по активным проектам?
        //Кроме того, хардкод, мягко говоря, не приветствуется. Лучше брать команды из app.config через ConfigurationManager       
        static Regex activeMask =new Regex(@"^active\w*");
        static Regex ratingMask = new Regex(@"^rating$");
        static string connectionString = @"Data Source =.\db\Database.db";
        //[THINKABOUT] Working directory всегда совпадает с директорией, из которой запущена конкретная сборка?
        static string Path = Directory.GetCurrentDirectory() + @"\reports";

        //[FIXME] Порядок следования модификаторов типа несколько иной
        //[FIXME] Переделать метод на работу с app.config
        static public void Execute(string command)
        {
            //[FIXME] Литералы, которые ТОЧНО не изменятся - в const string, остальные - в app.config
            if (command.ToLower() == "quit") 
                Environment.Exit(0);
            if (ratingMask.IsMatch(command))
            {
                //[THINKABOUT] А появится еще 10 команд - для каждой будем пилить новый метод?
                //Как можно забороть необходимость постоянно править класс? (Вспомни про O в SOLID)
                CreateRatingReport();
            }
            else
            {
                if (activeMask.IsMatch(command))
                {
                    command = command.Trim();
                    string[] parts = command.Split(' ');
                    if (parts.Length == 2) //[THINKABOUT] После появления третьего параметра опять изменять метод
                    {
                        //[THINKABOUT] if (DateTime.TryParse(parts[1], out DateTime time)) - такая нотация экономит строку
                        //Вообще, всю кучу строк из этого блока можно заменить на две
                        DateTime time;
                        if (DateTime.TryParse(parts[1], out time)) 
                            CreateActiveProjectsReport(time);
                        else
                        {
                            Console.WriteLine("Программа не смогла распознать дату, для отчета будет использована сегодняшняя дата.");
                            CreateActiveProjectsReport(DateTime.Now);
                        }
                    }
                    else
                    {
                        CreateActiveProjectsReport(DateTime.Now);
                    }
                }
            }
        }

        public static void CreateActiveProjectsReport(DateTime time)
        {
            try
            {
                DateTime filter = DateTime.Now;
                //[FIXME] Приведи реальный кейс, когда time может быть null
                if (time != null)
                {
                    filter = time;
                }

                //[THINKABOUT] Может, имеет смысл хотя бы простые инициализации делать с помощью var? Даже Java этому научилась, а мы-то моднее-молодёжнее!
                List<ActiveProjectsTemplate> projects = new List<ActiveProjectsTemplate>();
                
                //[FIXME] Почему в конструктор хардкодится одна строка, а в app.config - совершенно другая? Кстати, вообще почему строка подключения хардкодится?
                //В частности, я распаковал архив с двумя солюшенами as is - и Generator не может найти БД. Представь, что я такой обидчивый клиент, которому ты кинул сборки. Куда бежать? Что делать?
                using (SQLiteConnection conn = new SQLiteConnection(@"data source = .Parser\db\Database.db"))
                {

                    conn.Open();

                    //Console
                    Console.WriteLine(conn.ConnectionString);
                    //[FIXME] Если уж ты совсем не хочешь использовать ORM-слой всилу простоты запроса или каких-то других соображений - 
                    //вынеси хотя бы запрос в ресурсный файл. Изменение БД не должно каждый раз заставлять нас лезть переписывать логику класса. Да и не очень читаемо это всё.
                    //И зачем линковать EF6, если потом всё равно собираешь запрос и обрабатываешь выборку руками?
                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT Projects.Name,Projects.StartDate,ProjectOwners.Guid FROM Projects,ProjectOwners where ProjectOwners.Guid = Projects.OwnerGuid", conn))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                projects.Add(new ActiveProjectsTemplate
                                {
                                    //[FIXME] Очепятка
                                    ProejctName = reader.GetString(0),
                                    //[FIXME] Что будет, если из БД придет null?
                                    StartDate = reader.GetDateTime(1),
                                    //[FIXME] А теперь посмотри отчет и скажи - ты бы доверился человеку с именем "d6102a5f-f935-4f84-bfa1-1fe81b301ac6"?
                                    OwnerName = reader.GetString(2)
                                });
                            }
                        }
                    }
                }
                int size = projects.Count + 1;
                string[][] input = new string[size][];

                //[THINKABOUT] 10 раз сформировали отчет - 10 раз строится шапка. Что можно сделать?
                input[0] = new string[] { "Наименование проекта", "Дата старта проекта", "Владелец проекта" };

                if (!File.Exists(Path + @"\activeProjects.csv"))
                    File.Create(Path + @"\activeProjects.csv");


                using (StreamWriter sw = new StreamWriter(Path + @"\activeProjects.csv", false, Encoding.Default))
                {
                    for (int index = 1; index < size; index++)
                    {
                        string[] item = new string[3];
                        item[0] = projects[index - 1].ProejctName;
                        //[FIXME] А тут точно может быть null?
                        item[1] = projects[index - 1].StartDate.ToString() ?? "Unknown";
                        item[2] = projects[index - 1].OwnerName ?? "Unknown";

                        input[index] = item;
                    }
                    string delimiter = ";";

                    foreach (string[] str in input)
                    {
                        sw.WriteLine(string.Join(delimiter, str));
                    }
                };
                Console.WriteLine("Отчет сформирован успешно!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Возникла ошибка, отчет не сформирован!");
                Console.WriteLine(e.Message);
            }
        }

        //[FIXME] Святотатство в порядке модификаторов
        static public void CreateRatingReport()
        {

            try
            {
                //[FIXME] Ну var же есть! Хардкод убирай. И ты уверен, что я смогу подключиться к твоему data source? 
                List<RatingByFinished> projects = new List<RatingByFinished>();
                using (SQLiteConnection conn = new SQLiteConnection(@"data source = C:\Users\makmu\source\repos\BIA-Technologies (Parser)\BIA-Technologies (Parser)\bin\Debug\db\Database.db"))
                {

                    conn.Open();
                    Console.WriteLine(conn.ConnectionString);
                    using (SQLiteCommand cmd = new SQLiteCommand("select ProjectOwners.Name,count(Projects.Name) as 'ctr' from ProjectOwners,Projects where ProjectOwners.Guid = Projects.OwnerGuid group by Projects.OwnerGuid order by ctr desc", conn))
                    {
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                projects.Add(new RatingByFinished
                                {
                                    Owner = reader.GetString(0),
                                    Finished = reader.GetInt32(1),
                                });
                            }
                        }
                    }
                }
                int size = projects.Count + 1;
                string[][] input = new string[size][];

                //[FIXME] Шапка не совпадает с выводимыми полями
                input[0] = new string[] { "Наименование проекта", "Дата старта проекта", "Владелец проекта" };

                if (!File.Exists(Path + @"\rating.csv"))
                    File.Create(Path + @"\rating.csv");


                using (StreamWriter sw = new StreamWriter(Path + @"\rating.csv", false, Encoding.Default))
                {
                    //[THINKABOUT] StringBuilder?
                    for (int index = 1; index < size; index++)
                    {
                        string[] item = new string[3];
                        item[0] = index.ToString();
                        item[1] = projects[index - 1].Owner ?? "Unknown";
                        //[FIXME] Здесь когда-то будет null?
                        item[2] = projects[index - 1].Finished.ToString() ?? "Unknown";

                        input[index] = item;
                    }
                    //[FIXME] В конфиг или, на худой конец, в const
                    string delimiter = ";";

                    foreach (string[] str in input)
                    {
                        sw.WriteLine(string.Join(delimiter, str));
                    }
                };
                Console.WriteLine("Отчет сформирован успешно!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Возникла ошибка, отчет не сформирован!");
                Console.WriteLine(e.Message);
            }
        }
    }
    
}
