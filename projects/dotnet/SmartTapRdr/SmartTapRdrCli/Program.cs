﻿using SpringCard.LibCs;
using SpringCard.PCSC;
using SpringCard.GoogleVas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTapReadCli
{
    class Program : CLIProgram
    {
        const string ProgName = "SmartTapRdrCli";

        bool Quiet = false;
        bool JsonOutput = false;

        bool ListReaders = false;
        int ReaderIndex = -1;
        string ReaderName = null;

        string ConfigFileName = null;

        static int Main(string[] args)
        {
            Program p = new Program();
            return p.Run(args);
        }

        int Run(string[] args)
        {
            if (!ParseArgs(args))
                return 1;

            Logger.Debug("Loading the list of PC/SC readers");

            string[] ReaderNames = (new SCardReaderList()).Readers;

            if (ListReaders)
            {
                Console.WriteLine(string.Format("{0} PC/SC Reader(s) found", ReaderNames.Length));
                for (int i = 0; i < ReaderNames.Length; i++)
                    Console.WriteLine(string.Format("{0}: {1}", i, ReaderNames[i]));
                return 0;
            }


            if (!GoogleVasLicense.AutoLoad())
                Logger.Info("No license file");

            GoogleVasConfig config;

            if (ConfigFileName != null)
            {
                config = GoogleVasConfig.LoadFromJsonFile(ConfigFileName);
            }
            else
            {
                config = GoogleVasConfig.GoogleDemo();
            }

            if (ReaderName == null)
            {
                if (ReaderIndex < 0)
                    ReaderIndex = 0;

                Logger.Debug("Selecting the PC/SC reader at index {0}", ReaderIndex);

                if ((ReaderIndex >= ReaderNames.Length))
                {
                    Console.WriteLine("No PC/SC Reader at index {0}", ReaderIndex);
                    Console.WriteLine("Use " + ProgName + " --list-readers");
                    return 1;
                }

                ReaderName = ReaderNames[ReaderIndex];
            }

            Logger.Debug("Using PC/SC reader {0}", ReaderName);

            Logger.Debug("Opening the PC/SC reader");

            SCardReader reader = new SCardReader(ReaderName);

            Logger.Debug("Expecting to find a 'smartcard'");

            SCardChannel channel = new SCardChannel(reader);

            if (!channel.CardPresent)
            {
                ConsoleError("No NFC card or smartphone on the reader");
                return 1;
            }

            Logger.Debug("Connecting to the 'smartcard'");

            if (!channel.Connect())
            {
                ConsoleError("Failed to open the communication with the NFC card or smartphone");
                return 1;
            }

            GoogleVasError error;

            GoogleVasTerminal terminal = new GoogleVasTerminal(config);

            if (terminal.DoTransaction(channel, out GoogleVasData data, out error))
            {
                Logger.Debug("Message is OK");
            }
            else
            {
                OutputError("Read message failed, reason:\n", error);
            }
#if false

            GoogleVasTerminal controller = new GoogleVasTerminal(config);

            Logger.Debug("SelectOSE APDU");

            if (!controller.SelectOSE(channel, out bool mustSelectSmartTap, out error))
            {
                OutputError("SelectOSE failed", error);
                return 2;
            }

            if (!config.AllowSkippingSmartTap2Select || mustSelectSmartTap)
            {
                Logger.Debug("SelectSmartTap2 APDU");

                if (!controller.SelectSmartTap2(channel, out error))
                {
                    OutputError("SelectSmartTap2 failed", error);
                    return 2;
                }
            }

            if (config.UseEncryption)
            {
                Logger.Debug("NegotiateSecureSession APDU");

                if (!controller.NegotiateSecureSession(channel, true, out error))
                {
                    OutputError("NegotiateSecureSession failed", error);
                    return 2;
                }
            }

            Logger.Debug("GetData APDU");

            if (!controller.GetData(channel, out GoogleVasData data, out error))
            {
                OutputError("GetData failed", error);
                return 2;
            }
#endif
#if NON
            Logger.Debug("GetVasData APDU");

            byte[] buffer = GoogleVasController.GetVasData(channel, merchant, out error);
            if (buffer == null)
            {
                OutputError("GetVasData failed", error);
                return 2;
            }

            Logger.Debug("We have some data!");

            DateTime localTimeStamp = DateTime.UtcNow;

            if (JsonOutput)
            {
                Console.WriteLine("{");
                OutputField("result", error.ToString());
            }

            OutputField("nfcCryptogram", StrUtils.Base64Encode(buffer));
            OutputField("localTimestamp", localTimeStamp.ToString("yyyy-MM-ddTHH:mm:ssZ"), true);

            if (JsonOutput)
            {
                Console.WriteLine("}");
            }

            if (OnlineDecode)
            {
                Console.WriteLine("Querying {0}...", ServerUrl);

                RestClient restClient = new RestClient(ServerUrl);

                Dictionary<string, JSONObject> restData = new Dictionary<string, JSONObject>();
                restData["token"] = JSONObject.CreateString(OnlineToken);
                if (MerchantName != null)
                    restData["merchantName"] = JSONObject.CreateString(MerchantName);
                else
                    restData["merchantId"] = JSONObject.CreateString(StrUtils.Base64Encode(MerchantID));
                restData["nfcCryptogram"] = JSONObject.CreateString(StrUtils.Base64Encode(buffer));

                JSONObject restResponse = restClient.POST_Json(ServerApi, JSONObject.CreateObject(restData));

                if (restResponse == null)
                {
                    Console.WriteLine("Server error");
                    return 3;
                }

                string result = restResponse["result"].StringValue;

                if (JsonOutput)
                    Console.WriteLine("{");

                if (result.ToLower() == "success")
                {
                    string nfcMessage = restResponse["nfcMessage"].StringValue;
                    OutputField("nfcMessage", nfcMessage);
                    string nfcTimestamp = restResponse["nfcTimestamp"].StringValue;
                    OutputField("nfcTimestamp", nfcTimestamp, true);
                }
                else
                {
                    OutputField("result", result, true);
                }

                if (JsonOutput)
                    Console.WriteLine("}");

            }
#endif
            return 0;
        }

        void OutputError(string context, GoogleVasError error)
        {
            if (JsonOutput)
            {
                Console.WriteLine("{");
                OutputField("result", error.ToString(), true);
                Console.WriteLine("}");
            }
            else
            {
                ConsoleError(context);
                ConsoleError(error.ToString());
            }
        }

        void OutputField(string fieldName, string fieldValue, bool last = false)
        {
            if (JsonOutput)
            {
                Console.WriteLine("\t\"{0}\": \"{1}\"{2}", fieldName, fieldValue, last ? "" : ",");
            }
            else
            {
                Console.Write("{0}: ", fieldName);
                ConsoleColor(System.ConsoleColor.White);
                Console.Write("{0}", fieldValue);
                ConsoleColor();
                Console.WriteLine();
            }
        }

        void Usage()
        {
            Console.WriteLine("Usage: {0} [PARAMETERS] [[OPTIONS]]", ProgName);
            Console.WriteLine();
            ConsoleTitle("PARAMETERS");
            Console.WriteLine("  -c --config=<TERMINAL CONFIGURATION FILE>");
            Console.WriteLine("  -N --merchant-name=<MERCHANT NAME>");
            Console.WriteLine("  -I --merchant-id=<MERCHANT ID (base64)>");
            Console.WriteLine();
            ConsoleTitle("To enumerate the PC/SC Reader(s), use");
            Console.WriteLine("  " + ProgName + " --list-readers");
            Console.WriteLine("  " + ProgName + " -L");
            Console.WriteLine();
            ConsoleTitle("OPTIONS:");
            Console.WriteLine("  -j --json    : decorate the output as JSON");
            Console.WriteLine("  -i --reader-index=<INDEX> (default is 0)");
            Console.WriteLine("  -n --reader-name=<NAME>");
            Console.WriteLine("  -o --online  : use springpass.springcard.com to decode the cryptogram");
            Console.WriteLine("  -t --token   : authentication token for springpass.springcard.com");
            Console.WriteLine("  -q --quiet");
            Console.WriteLine("  -v --verbose");
            Console.WriteLine();
        }

        bool ParseArgs(string[] args)
        {
            int c;

            List<LongOpt> options = new List<LongOpt>();
            options.Add(new LongOpt("config", Argument.Required, null, 'c'));
            options.Add(new LongOpt("list-readers", Argument.No, null, 'l'));
            options.Add(new LongOpt("reader-index", Argument.Required, null, 'i'));
            options.Add(new LongOpt("reader-name", Argument.Required, null, 'n'));
            options.Add(new LongOpt("json", Argument.No, null, 'j'));
            options.Add(new LongOpt("quiet", Argument.No, null, 'q'));
            options.Add(new LongOpt("verbose", Argument.Optional, null, 'v'));
            options.Add(new LongOpt("help", Argument.No, null, 'h'));

            Getopt g = new Getopt(ProgName, args, "c:li:n:jqv::h", options.ToArray());
            g.Opterr = true;

            while ((c = g.getopt()) != -1)
            {
                string arg = g.Optarg;

                switch (c)
                {
                    case 'c':
                        ConfigFileName = arg;
                        break;
                    case 'l':
                        ListReaders = true;
                        break;
                    case 'i':
                        ReaderIndex = int.Parse(arg);
                        break;
                    case 'n':
                        ReaderName = arg;
                        break;
                    case 'j':
                        JsonOutput = true;
                        break;
                    case 'q':
                        Quiet = true;
                        break;
                    case 'v':
                        Logger.ConsoleLevel = Logger.Level.Trace;
                        if (arg != null)
                        {
                            int level;
                            if (int.TryParse(arg, out level))
                                Logger.ConsoleLevel = Logger.IntToLevel(level);
                        }
                        break;

                    case 'h':
                        Usage();
                        return false;

                    default:
                        Console.WriteLine("Invalid argument on command line");
                        Console.WriteLine("Try " + ProgName + " --help");
                        return false;
                }
            }

            if (ListReaders && ((ReaderIndex != -1) || (ReaderName != null) || (ConfigFileName != null)))
            {
                Console.WriteLine("You can't both list the readers and specify other parameters");
                Console.WriteLine("Try " + ProgName + " --help");
                return false;
            }

            if (!ListReaders)
            {
                if ((ReaderIndex != -1) || (ReaderName != null))
                {
                    Console.WriteLine("You can't both specify a reader name and a reader index");
                    Console.WriteLine("Try " + ProgName + " --help");
                    return false;
                }

                /*
                if (ConfigFileName == null)
                {
                    Console.WriteLine("You must specify a terminal configuration file");
                    Console.WriteLine("Try " + ProgName + " --help");
                    return false;
                }
                */
            }

            return true;
        }
    }
}
