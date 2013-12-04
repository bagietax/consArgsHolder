using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace consArgsHolder
{
    class Program
    {

        public class Options
        {
            [Argument('c',"character",false,"Odpowiada za coś tam",false)]
            public string Name
            {
                get;
                set;
            }

            [Argument('d', "dupa", true, "blablabla ",false)]
            public int SubName
            {
                get;
                set;
            }

            [Argument('e', "", true, "blablabla ",true)]
            public bool TylkoFlaga
            {
                get;
                set;
            }

            [Argument('r', "", false, "blablabla ",false)]
            public string WymaganyAleNiePodane
            {
                get;
                set;
            }
        }


        static void Main(string[] args)
        {
            Options options = new Options();
            ArgumentManager.LoadArgsSetObject(args,options);

            Console.ReadLine();
           
        }

    }


   
    /// <summary>
    /// Atrubut używany do opisywania arguemntów
    /// </summary>

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class Argument : System.Attribute
    {
        public char CharName {  get; private set; }
        public string LongName { get; private set; }
        public bool IsReq {  get; private set; }
        private string _comment;
        public bool IsOnlyFlag { get; private set; }
        public string PropertyName;



        public Argument(char charName)
        {
            CharName = charName;
            LongName = "";
            IsReq = false;
            _comment = "";
            IsOnlyFlag = false;
        }

        public Argument(char charName,  bool isRequired)
        {
            CharName = charName;
            LongName = "";
            IsReq = isRequired;
            _comment = "";
            IsOnlyFlag = false;
        }

        public Argument(char charName,  bool isRequired, bool isOnlyFlagNoParams)
        {
            CharName = charName;
            LongName = "";
            IsReq = isRequired;
            _comment = "";
            IsOnlyFlag = isOnlyFlagNoParams;
        }
        public Argument(char charName, string longName, bool isRequired, string comment,bool isOnlyFlagNoParams)
        {
            CharName = charName;
            LongName = longName;
            IsReq = isRequired;
            _comment = comment;
            IsOnlyFlag = isOnlyFlagNoParams;
        }
    }

    public class ArgumentIsRequired : Exception
    {
        Argument _reqArg;
        public ArgumentIsRequired(string message, Exception innerExc,Argument reqArg)
            : base(message, innerExc)
        { 
            _reqArg = reqArg;  
        }
    
    }

    public class FlagPropertyIsNotBoolean : Exception
    {
        PropertyInfo _property;
        public FlagPropertyIsNotBoolean(string message, Exception innerExc, PropertyInfo prop)
            : base(message, innerExc)
        {
            _property = prop;
        }

    }



    public static class ArgumentManager
    {
        private static List<Argument> allArguments=new List<Argument>();
        private static Dictionary<string, string> argsPair = new Dictionary<string, string>();

        public static void LoadArgsSetObject(string[] args, object someClass)
        {
            LoadPropertiesFromClass(someClass);
            FindArgsWithParamemters(args);
            SetClassProperties(someClass);

        }

        private static void SetClassProperties(object someObject)
        {
            foreach (var argument in allArguments)
            {
                PropertyInfo prop = someObject.GetType().GetProperty(argument.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (null != prop && prop.CanWrite)
                {
                    if (argsPair.ContainsKey(argument.CharName.ToString()) || argsPair.ContainsKey(argument.LongName.ToString()))
                    {
                        if (argument.IsOnlyFlag)
                        {
                            if(prop.PropertyType!=typeof(bool))
                                throw new FlagPropertyIsNotBoolean(String.Format("Property {0} is taged like flag but isnt bool", prop.Name),null,prop);
                            prop.SetValue(someObject,
                               true,
                            null);
                        }
                        else
                        {
                            string value = string.Empty ;
                            try
                            {
                                
                                if (argsPair.ContainsKey(argument.CharName.ToString()))
                                {
                                    value =argsPair[argument.CharName.ToString()];
                                    prop.SetValue(someObject,
                                        Convert.ChangeType(value, prop.PropertyType),
                                    null);
                                }
                                else
                                {
                                    value=argsPair[argument.LongName.ToString()];
                                    prop.SetValue(someObject,
                                        Convert.ChangeType(value, prop.PropertyType),
                                    null);
                                }
                            }
                            catch(Exception e)
                            {

                                throw new Exception(String.Format("Parsing problem with: -[{0}|{1}] value: \"{2}\"  to type: {3} ", argument.CharName, argument.LongName, value, prop.PropertyType)
                                    );
                            }
                        }
                    }
                    else if(!argsPair.ContainsKey(argument.CharName.ToString()) && argument.IsReq)
                    {
                        throw new ArgumentIsRequired(String.Format("Parameters \"{0}\" is requied but is not in args", argument.CharName),null,argument);
                    }
                }
            }
        }

        
        private static void LoadPropertiesFromClass(object someClass)
        {
            var typeOfClass = someClass.GetType();
            var aaasf = typeOfClass
            .GetProperties();
            foreach (var item in aaasf)
            {
                var arg = (Argument)
                    item.GetCustomAttributes(false)
                    .FirstOrDefault(a => a.GetType().Name == typeof(Argument).Name);
                arg.PropertyName = item.Name;
                allArguments.Add(arg);                
            }            
        }

        private static void FindArgsWithParamemters(string[] args)
        {
            int i=0;
            foreach (string firstArgs in args)
            {
                string arg2;
                if(i+1>args.Count()-1)
                    arg2=string.Empty;
                else
                    arg2=args[i+1];
                if (firstArgs.StartsWith("--"))
                { 
                    AddToDic(firstArgs,arg2,2);
                    
                }
                else if(firstArgs.StartsWith("-"))
                {
                    AddToDic(firstArgs, arg2, 1);
                }
                i++;
            }
        }

        private static void AddToDic(string firstArgs,string secondArgs, int p)
        {
            if (p == 1)
            { 
                
                string trimmedSwitch= firstArgs.Replace("-",string.Empty);
                if(!argsPair.ContainsKey(trimmedSwitch) && !secondArgs.StartsWith("-"))
                {
                    if(!secondArgs.StartsWith("-"))
                    {
                        argsPair.Add(trimmedSwitch, secondArgs);
                    }
                    else
                    {
                        argsPair.Add(trimmedSwitch, null);
                    }

                }
            }
            else if (p == 2)
            {
                string trimmedSwitch = firstArgs.Replace("--", string.Empty);
                if (!argsPair.ContainsKey(trimmedSwitch))
                {
                    if (!secondArgs.StartsWith("--"))
                    {
                        argsPair.Add(trimmedSwitch, secondArgs);
                    }
                    else
                    {
                        argsPair.Add(trimmedSwitch, null);
                    }

                }
            }
        }
    }
}
