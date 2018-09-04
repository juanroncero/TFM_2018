using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Bittrex;
using Bittrex.Data;
using consumer.ENTITIES;
using HtmlAgilityPack;
using InfoProxy;
using mshtml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using Goldfinger.Bittrex;
//using Goldfinger.Bittrex.PublicApi;
//using mshtml;


namespace consumer
{
    public partial class CargaDatos : Form
    {
        private string Key = "XXXX";
        private string Secret = "YYYY";
        //private string url = "https://bittrex.com/api/v1.1/account/getbalances?apikey=apikey";
        private string pepe = "https://bittrex.com/api/v1.1/public/getmarkets";

        BDD bdd = new BDD();

        //int contSegTransacciones = 0;
        //int contSegValorMonedas = 0;
        //int contSegRedesSociales = 0;

        int contSegundos = 0;
        int contSegundosBallenas = 0;

        //private SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindows();


        // API CALLS
        Exchange ex = new Exchange();
        ExchangeContext p = new ExchangeContext();

        List<TIPO_MONEDA> monedas = new List<TIPO_MONEDA>();

        public CargaDatos()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {


                // API BITSTAMP
                p.ApiKey = Key;
                p.Secret = Secret;
                p.QuoteCurrency = "USDT"; // MONEDA ORIGEN DE CAMBIO - 
                ex.Initialise(p);

                monedas = bdd.getMonedas();

                // 2. GUARDAMOS ULTIMAS TRANSACCIONES (BITCOINT, RIPPLE, ETHERUM)
                guardaTransaccionesOptimizado2();

                procesaBallenas();

                // 3. FUTUROS
                //guardaOrdenesFuturas();


            }
            catch (Exception ex)
            {


            }
        }


        private void procesaBallenas()
        {
            List<ballena> ballenas = new List<ballena>();
            //string walletOrigen = "rGcyM8aFJwLEFajEeCJwtQJsCTYt9qXKsu";
            string url = "https://xrpcharts.ripple.com/#/graph/";

            SHDocVw.ShellWindows shellWindows;

            try
            {
                // 1. CERRAMOS TODAS LAS INSTANCIAS DE IE ABIERTAS
                shellWindows = new SHDocVw.ShellWindows();
                foreach (SHDocVw.WebBrowser ie in shellWindows)
                {
                    ie.Quit();
                }


                List<string> cuentasBallenas = bdd.getCuentasBallenas();

                foreach (string cuentaBallena in cuentasBallenas)
                {
                    Process.Start("IExplore.exe", url + cuentaBallena);

                    System.Threading.Thread.Sleep(5000); // ESPERAMOS CARGA

                    shellWindows = new SHDocVw.ShellWindows();

                    foreach (SHDocVw.WebBrowser ie in shellWindows)
                    {
                        // 1. Obtenemos codigo fuente
                        HTMLDocument doc = ie.Document as mshtml.HTMLDocument;

                        if (doc != null)
                        {
                            string docBody = doc.body.outerHTML;
                            docBody = doc.body.innerHTML;

                            // 2. Parseamos resultados de html
                            if (docBody != null)
                            {
                                int contCell = 0;

                                HtmlAgilityPack.HtmlDocument hap = new HtmlAgilityPack.HtmlDocument();
                                hap.LoadHtml(docBody);

                                if (hap.DocumentNode.SelectNodes("//table[@class='outertable']") != null)
                                {
                                    foreach (HtmlNode table in hap.DocumentNode.SelectNodes("//table[@class='outertable']"))
                                    {
                                        if (!table.InnerHtml.ToString().Contains("toprow"))
                                        {
                                            foreach (HtmlNode tbody in table.SelectNodes("tbody"))
                                            {
                                                foreach (HtmlNode row in tbody.SelectNodes("tr"))
                                                {
                                                    ballena b = new ballena();
                                                    b.walletOrigen = cuentaBallena;

                                                    contCell = 0;
                                                    foreach (HtmlNode cell in row.SelectNodes("th|td"))
                                                    {
                                                        if (contCell == 0)
                                                        {
                                                            if (cell.SelectNodes("//div[@title]") != null)
                                                            {
                                                                b.tipoPperacion = cell.SelectNodes("//div[@title]").LastOrDefault().Attributes["title"].Value;
                                                                contCell++;
                                                            }
                                                        }
                                                        else
                                                        {

                                                            foreach (HtmlNode span in cell.SelectNodes(".//span[@class]"))
                                                            {
                                                                string attributeValue = span.GetAttributeValue("class", "");

                                                                switch (attributeValue)
                                                                {
                                                                    case "bold amount small":
                                                                        b.cantidad = span.InnerText.Replace(",", "");

                                                                        if (b.cantidad.Contains("59899"))
                                                                        { 
                                                                        
                                                                        }
                                                                        break;
                                                                    case "light small darkgray":
                                                                        b.moneda = span.InnerText;
                                                                        break;
                                                                    case "light small mediumgray date":
                                                                        //b.fecha = span.InnerText;
                                                                        string auxFecha = span.GetAttributeValue("title", "");
                                                                        IFormatProvider enUsDateFormat = new CultureInfo("en-US").DateTimeFormat;
                                                                        DateTime aux = Convert.ToDateTime(auxFecha, enUsDateFormat);
                                                                        b.fecha = aux;
                                                                        break;
                                                                    case "light address right":
                                                                        b.walleDestino = span.InnerText;
                                                                        break;
                                                                }
                                                            }

                                                            ballenas.Add(b);

                                                            

                                                            // SI LA TRANSACCION > 999.999 
                                                            if (Convert.ToDouble(b.cantidad.Replace(".",",")) > 999999)
                                                            {
                                                                // DAMOS DE ALTA LA CUENTA DESTINO EN CUENTAS BALLENA SI NO EXISTE
                                                                bdd.altaBallena(b);

                                                                // DAMOS DE ALTA LA TRANSACCION SI NO EXISTE
                                                                bdd.altaTransaccion(b);

                                                                lstBallenas.Items.Add("TRANSACCION DE " + b.cantidad + " XRP DESDE " + b.walletOrigen);

                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        ie.Quit();
                    }


                }

            }
            catch (Exception ex)
            {


            }
        }



        private void testBallenas2()
        {
            List<ballena> ballenas = new List<ballena>();

            try
            {

                string walletOrigen = "rGcyM8aFJwLEFajEeCJwtQJsCTYt9qXKsu";
                string url = "https://xrpcharts.ripple.com/#/graph/" + walletOrigen;

                //Process.Start("IExplore.exe", url);
                SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindows();

                foreach (SHDocVw.WebBrowser ie in shellWindows)
                {
                    // 1. Obtenemos codigo fuente
                    HTMLDocument doc = ie.Document as mshtml.HTMLDocument;

                    if (doc != null)
                    {

                        string docBody = doc.body.outerHTML;
                        docBody = doc.body.innerHTML;

                        // 2. Parseamos resultados de html
                        if (docBody != null)
                        {
                            int contCell = 0;

                            HtmlAgilityPack.HtmlDocument hap = new HtmlAgilityPack.HtmlDocument();
                            hap.LoadHtml(docBody);

                            if (hap.DocumentNode.SelectNodes("//table[@class='outertable']") != null)
                            {
                                foreach (HtmlNode table in hap.DocumentNode.SelectNodes("//table[@class='outertable']"))
                                {
                                    if (!table.InnerHtml.ToString().Contains("toprow"))
                                    {
                                        foreach (HtmlNode tbody in table.SelectNodes("tbody"))
                                        {
                                            foreach (HtmlNode row in tbody.SelectNodes("tr"))
                                            {
                                                ballena b = new ballena();
                                                b.walletOrigen = walletOrigen;

                                                contCell = 0;
                                                foreach (HtmlNode cell in row.SelectNodes("th|td"))
                                                {
                                                    if (contCell == 0)
                                                    {
                                                        if (cell.SelectNodes("//div[@title]") != null)
                                                        {
                                                            b.tipoPperacion = cell.SelectNodes("//div[@title]").LastOrDefault().Attributes["title"].Value;
                                                            contCell++;
                                                        }
                                                    }
                                                    else
                                                    {

                                                        foreach (HtmlNode span in cell.SelectNodes(".//span[@class]"))
                                                        {
                                                            string attributeValue = span.GetAttributeValue("class", "");

                                                            switch (attributeValue)
                                                            {
                                                                case "bold amount small":
                                                                    b.cantidad = span.InnerText;
                                                                    break;
                                                                case "light small darkgray":
                                                                    b.moneda = span.InnerText;
                                                                    break;
                                                                case "light small mediumgray date":
                                                                    //b.fecha = span.InnerText;
                                                                    //b.fecha = span.GetAttributeValue("title", "");
                                                                    break;
                                                                case "light address right":
                                                                    b.walleDestino = span.InnerText;
                                                                    break;
                                                            }
                                                        }

                                                        ballenas.Add(b);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {


            }
        }

        private void testBallenas()
        {
            try
            {

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


                string url = "https://xrpcharts.ripple.com/#/graph/rGcyM8aFJwLEFajEeCJwtQJsCTYt9qXKsu";
                var uri = String.Format(url);


                var request = WebRequest.Create(uri) as HttpWebRequest;
                var response = request.GetResponse() as HttpWebResponse;

                Encoding encoding = Encoding.GetEncoding(response.CharacterSet);

                using (var responseStream = response.GetResponseStream())

                using (var streamReader = new StreamReader(responseStream, encoding))
                {
                    var responseText = streamReader.ReadToEnd();

                }

                //Uri uri = new Uri(url);
                //WebRequest webRequest = WebRequest.Create(uri);
                //WebResponse webResponse = webRequest.GetResponse();

                //var response = request.GetResponse() as HttpWebResponse;
                //Encoding encoding = Encoding.GetEncoding(response.CharacterSet);

                //using (var responseStream = response.GetResponseStream())

                //using (var streamReader = new StreamReader(responseStream, encoding))
                //{
                //    var responseText = streamReader.ReadToEnd();
                //}


            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// RECOGE EN COMPRAS Y VENTAS, CANTIDAD DE OPERACIONES CON 1-15% DE DIFERENCIA AL PRECIO
        /// </summary>
        private void guardaOrdenesFuturas()
        {
            try
            {


                List<TRANSACCION> futuros = new List<TRANSACCION>();

                int[] difPorcentajesCompra = new int[20];
                int[] difPorcentajesVenta = new int[20];

                int cont = 0;


                foreach (TIPO_MONEDA m in monedas)
                {
                    //m.moneda = "xrp";
                    //m.precioActualCompra = 0.30;

                    PublicMarket api = new PublicMarket();
                    OrderBook res = api.GetOrderBook(m.moneda);

                    List<string[]> bids = res.bids;
                    foreach (string[] s in bids)
                    {
                        TRANSACCION t = new TRANSACCION();
                        t.tipoTransaccion = ENUMS.ENUMS.TIPO_TRANSACCION.COMPRA;
                        t.price = Convert.ToDecimal(s[0].ToString().Replace(".", ","));
                        t.amount = Convert.ToDecimal(s[1].ToString().Replace(".", ","));
                        //futuros.Add(t);

                        double porcentajeDif = (m.precioActualCompra * 100 / Convert.ToDouble(t.price)) - 100;


                        for (double i = 0.01; i < 1; i = i + 0.1)
                        {
                            if ((porcentajeDif > i) && (porcentajeDif < i + 1))
                            {
                                difPorcentajesCompra[cont]++;
                                cont++;
                                break;
                            }
                        }

                    }


                    List<string[]> asks = res.asks;
                    foreach (string[] s in bids)
                    {
                        TRANSACCION t = new TRANSACCION();
                        t.tipoTransaccion = ENUMS.ENUMS.TIPO_TRANSACCION.VENTA;
                        t.price = Convert.ToDecimal(s[0].ToString());
                        t.amount = Convert.ToDecimal(s[1].ToString());
                        //futuros.Add(t);

                        double porcentajeDif = m.precioActualCompra / Convert.ToDouble(t.price);

                        for (int i = 0; i < 20; i++)
                        {
                            if ((porcentajeDif > i) && (porcentajeDif < i + 1))
                                difPorcentajesVenta[i]++;
                        }
                    }


                    // ALTA MASIVA
                    // TIPO_OPERACION TIMESTAMP FECHA DIF(%) NUMERO_TRANSACCIONES
                    // 1-15%





                }

            }
            catch (Exception ex)
            {

            }
        }

        private void altaMasivaFuturos(List<TRANSACCION> transacciones)
        {
            try
            {
                DataTable dtTransacciones = getDataTableTransaciones();

                foreach (TRANSACCION t in transacciones)
                {
                    DataRow row = dtTransacciones.NewRow();
                    row["AMOUNT"] = t.amount;
                    row["FECHA"] = t.fecha;
                    row["TIMESPAN"] = t.date;
                    row["PRICE"] = t.price;
                    row["TIPOTRANSACCION"] = t.type;
                    row["ID_MONEDA"] = t.idMoneda;
                    dtTransacciones.Rows.Add(row);
                }


                bdd.altaEstructuraMasiva(dtTransacciones, "[CRYPTOCOIN].[dbo].[BITSTAMP_TRANSACCIONES]");

            }
            catch (Exception ex)
            {

            }
        }


        //private void altaMasivaFuturos(List<TRANSACCION> transacciones)
        //{
        //    try
        //    {
        //        DataTable dtTransacciones = getDataTableTransaciones();

        //        foreach (TRANSACCION t in transacciones)
        //        {
        //            DataRow row = dtTransacciones.NewRow();
        //            row["AMOUNT"] = t.amount;
        //            row["FECHA"] = t.fecha;
        //            row["TIMESPAN"] = t.date;
        //            row["PRICE"] = t.price;
        //            row["TIPOTRANSACCION"] = t.type;
        //            row["ID_MONEDA"] = t.idMoneda;
        //            dtTransacciones.Rows.Add(row);
        //        }


        //        bdd.altaEstructuraMasiva(dtTransacciones, "[CRYPTOCOIN].[dbo].[BITSTAMP_TRANSACCIONES]");

        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        private void altaMasivaTransacciones(List<TRANSACCION> transacciones)
        {
            try
            {
                DataTable dtTransacciones = getDataTableTransaciones();

                foreach (TRANSACCION t in transacciones)
                {
                    DataRow row = dtTransacciones.NewRow();
                    row["AMOUNT"] = t.amount;
                    row["FECHA"] = t.fecha;
                    row["TIMESPAN"] = t.date;
                    row["PRICE"] = t.price;
                    row["TID"] = t.tid;
                    row["TIPOTRANSACCION"] = t.type;
                    row["ID_MONEDA"] = t.idMoneda;
                    dtTransacciones.Rows.Add(row);
                }


                bdd.altaEstructuraMasiva(dtTransacciones, "[CRYPTOCOIN].[dbo].[BITSTAMP_TRANSACCIONES]");

            }
            catch (Exception ex)
            {

            }
        }


        private DataTable getDataTableFuturos()
        {
            DataTable dtTrans = new DataTable();

            try
            {

                DataColumn AMOUNT = new DataColumn();
                AMOUNT.DataType = System.Type.GetType("System.Double");
                AMOUNT.ColumnName = "AMOUNT";
                dtTrans.Columns.Add(AMOUNT);

                DataColumn FECHA = new DataColumn();
                FECHA.DataType = System.Type.GetType("System.DateTime");
                FECHA.ColumnName = "FECHA";
                dtTrans.Columns.Add(FECHA);

                DataColumn TIMESPAN = new DataColumn();
                TIMESPAN.DataType = System.Type.GetType("System.Int32");
                TIMESPAN.ColumnName = "TIMESPAN";
                dtTrans.Columns.Add(TIMESPAN);

                DataColumn PRICE = new DataColumn();
                PRICE.DataType = System.Type.GetType("System.Decimal");
                PRICE.ColumnName = "PRICE";
                dtTrans.Columns.Add(PRICE);

                DataColumn TIPOTRANSACCION = new DataColumn();
                TIPOTRANSACCION.DataType = System.Type.GetType("System.Int16");
                TIPOTRANSACCION.ColumnName = "TIPOTRANSACCION";
                dtTrans.Columns.Add(TIPOTRANSACCION);

                DataColumn ID_MONEDA = new DataColumn();
                ID_MONEDA.DataType = System.Type.GetType("System.Int32");
                ID_MONEDA.ColumnName = "ID_MONEDA";
                dtTrans.Columns.Add(ID_MONEDA);

                return dtTrans;

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private DataTable getDataTableTransaciones()
        {
            DataTable dtTrans = new DataTable();

            try
            {

                DataColumn AMOUNT = new DataColumn();
                AMOUNT.DataType = System.Type.GetType("System.Double");
                AMOUNT.ColumnName = "AMOUNT";
                dtTrans.Columns.Add(AMOUNT);

                DataColumn FECHA = new DataColumn();
                FECHA.DataType = System.Type.GetType("System.DateTime");
                FECHA.ColumnName = "FECHA";
                dtTrans.Columns.Add(FECHA);

                DataColumn TIMESPAN = new DataColumn();
                TIMESPAN.DataType = System.Type.GetType("System.Int32");
                TIMESPAN.ColumnName = "TIMESPAN";
                dtTrans.Columns.Add(TIMESPAN);

                DataColumn PRICE = new DataColumn();
                PRICE.DataType = System.Type.GetType("System.Decimal");
                PRICE.ColumnName = "PRICE";
                dtTrans.Columns.Add(PRICE);

                DataColumn TID = new DataColumn();
                TID.DataType = System.Type.GetType("System.Int32");
                TID.ColumnName = "TID";
                dtTrans.Columns.Add(TID);

                DataColumn TIPOTRANSACCION = new DataColumn();
                TIPOTRANSACCION.DataType = System.Type.GetType("System.Int16");
                TIPOTRANSACCION.ColumnName = "TIPOTRANSACCION";
                dtTrans.Columns.Add(TIPOTRANSACCION);

                DataColumn ID_MONEDA = new DataColumn();
                ID_MONEDA.DataType = System.Type.GetType("System.Int32");
                ID_MONEDA.ColumnName = "ID_MONEDA";
                dtTrans.Columns.Add(ID_MONEDA);

                return dtTrans;

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private void guardaTransaccionesOptimizado2()
        {

            //string url = "https://www.bitstamp.net/api/v2/transactions/xrpusd/";

            string source_url = "https://www.bitstamp.net/api/v2/transactions/";


            List<TRANSACCION> ultimaTransacciones = new List<TRANSACCION>();
            //ultimas2000Transacciones = bdd.getTransacciones(8000);

            int contTotal = 0;

            List<TRANSACCION> tts = new List<TRANSACCION>();

            // ALERTAS
            List<TRANSACCION> ttsParaAlertas = new List<TRANSACCION>();


            try
            {

                int contT = 0;

                foreach (TIPO_MONEDA m in monedas)
                {

                    m.precioActualCompra = 0;
                    m.precioActualVenta = 0;

                    string finalURL = source_url + m.moneda.ToLower() + "usd";

                    using (var webpage = new WebClient())
                    {
                        webpage.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                        var json = webpage.DownloadString(finalURL);

                        TRANSACCION[] tt = new JavaScriptSerializer().Deserialize<TRANSACCION[]>(json);

                        // TODO - COGER LAS ULTIMAS TT.LENGTH DE ESA MONEDA
                        ultimaTransacciones = bdd.getTransacciones(tt.Length, m.ID);

                        foreach (TRANSACCION t in tt)
                        {
                            t.timespan = Convert.ToInt32(t.date);
                            t.fecha = UnixTimeStampToDateTime(t.timespan);
                            t.idMoneda = m.ID;

                            if (!existeTransaccionEnBdd(ultimaTransacciones, t.tid))
                            {
                                tts.Add(t);
                                contT++;

                                // PONEMOS PRECIO ACTUAL DE CADA MONEDA
                                if (t.tipoTransaccion == ENUMS.ENUMS.TIPO_TRANSACCION.COMPRA)
                                {
                                    // EN COMPRAS COGEMOS EL MÁS CARO
                                    if (m.precioActualCompra < Convert.ToDouble(t.price))
                                        m.precioActualCompra = Convert.ToDouble(t.price);
                                }
                                else
                                {
                                    // EN VENTAS COGEMOS EL MAS BARATO
                                    if (m.precioActualVenta > Convert.ToDouble(t.price))
                                        m.precioActualVenta = Convert.ToDouble(t.price);
                                }

                                // ALERTAS 
                                switch (m.ID)
                                {
                                    // RIPPLE
                                    case 1:
                                        if (t.amount > 20)
                                            ttsParaAlertas.Add(t);
                                        break;
                                    // BITCOIN
                                    case 5:
                                        if (t.amount > 49000)
                                            ttsParaAlertas.Add(t);
                                        break;
                                }

                            }

                            contTotal++;
                        }
                    }
                }

                altaMasivaTransacciones(tts);

                if (listBox1.Items.Count > 100)
                    listBox1.Items.Clear();

                if (lstBallenas.Items.Count > 100)
                    lstBallenas.Items.Clear();

                if (ttsParaAlertas.Count > 0)
                    enviarEmail(ttsParaAlertas);

                listBox1.Items.Add(DateTime.Now.ToString() + " - " + contT + " transacciones guardadas.");

            }
            catch (Exception ex)
            {

            }
        }

        private void enviarEmail(List<TRANSACCION> tt)
        {
            try
            {
                string contenidoHTML = string.Empty;


                string from = "XXXX";
                string destinatario = "DDEDEDE";

                MailMessage mimail = new MailMessage();
                mimail.IsBodyHtml = true;

                mimail.Body = contenidoHTML;
                mimail.From = new MailAddress(from);
                mimail.To.Add(destinatario);
                mimail.To.Add("ronosm@gmail.com");
                mimail.To.Add("udo.llorens@gmail.com");
                mimail.To.Add("Argiles79@gmail.com");
                mimail.BodyEncoding = Encoding.UTF8;

                string mensaje = string.Empty;

                mensaje = "<TABLE>";
                mensaje += "<TR><TD>FECHA</TD><TD>OPERACION</TD><TD>MONEDA</TD><TD>CANTIDAD</TD><TD>PRECIO</TD>";


                foreach (TRANSACCION t in tt)
                {

                    string moneda = string.Empty;
                    switch (t.idMoneda)
                    {
                        case 1:
                            moneda = "BITCOIN";
                            break;
                        case 2:
                            moneda = "ETHERUM";
                            break;
                        case 5:
                            moneda = "RIPPLE";
                            break;
                        case 6:
                            moneda = "LITCOIN";
                            break;
                    }

                    string OPERACION = string.Empty;
                    if (t.type == "0")
                        OPERACION = "COMPRA";
                    else
                        OPERACION = "VENTA";

                    mensaje += "<TR><TD>" + t.fecha + " </TD><TD> " + OPERACION + " </TD><TD> " + moneda + " - </TD><TD> " + t.amount + " </TD><TD> " + t.price + "</TD>";

                }

                mensaje += "</TABLE>";

                mimail.Subject = "MERCADO ACTIVO";
                mimail.Body = mensaje.ToString();



                SmtpClient MyMail = new SmtpClient("X.X.X.X");
                MyMail.DeliveryMethod = SmtpDeliveryMethod.Network;

                try
                {
                    MyMail.Send(mimail);
                }
                catch (Exception ex)
                {
                }

            }
            catch (Exception ex)
            {

            }
        }


        private void guardaTransaccionesOptimizado()
        {

            //string url = "https://www.bitstamp.net/api/v2/transactions/xrpusd/";

            string source_url = "https://www.bitstamp.net/api/v2/transactions/";


            List<TRANSACCION> ultimas2000Transacciones = new List<TRANSACCION>();
            //ultimas2000Transacciones = bdd.getTransacciones(2000);

            try
            {
                foreach (TIPO_MONEDA m in monedas)
                {
                    string finalURL = source_url + m.moneda.ToLower() + "usd";

                    using (var webpage = new WebClient())
                    {
                        webpage.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                        var json = webpage.DownloadString(finalURL);

                        TRANSACCION[] tt = new JavaScriptSerializer().Deserialize<TRANSACCION[]>(json);

                        foreach (TRANSACCION t in tt)
                        {
                            t.timespan = Convert.ToInt32(t.date);
                            t.fecha = UnixTimeStampToDateTime(t.timespan);
                            t.idMoneda = m.ID;

                            if (!existeTransaccionEnBdd(ultimas2000Transacciones, t.tid))
                                bdd.altaTransaccionBitstamp(t);
                        }
                    }

                }
            }
            catch (Exception ex)
            {

            }
        }

        private bool existeTransaccionEnBdd(List<TRANSACCION> transacciones, string tid)
        {
            bool res = false;
            try
            {

                foreach (TRANSACCION t in transacciones)
                {
                    if (t.tid == tid)
                    {
                        res = true;
                        break;
                    }
                }

                return res;

            }
            catch (Exception ex)
            {

                return false;
            }
        }


        private void guardaTransacciones()
        {

            //string url = "https://www.bitstamp.net/api/v2/transactions/xrpusd/";

            string source_url = "https://www.bitstamp.net/api/v2/transactions/";

            try
            {
                foreach (TIPO_MONEDA m in monedas)
                {
                    string finalURL = source_url + m.moneda.ToLower() + "usd";

                    using (var webpage = new WebClient())
                    {
                        webpage.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
                        var json = webpage.DownloadString(finalURL);



                        TRANSACCION[] tt = new JavaScriptSerializer().Deserialize<TRANSACCION[]>(json);

                        foreach (TRANSACCION t in tt)
                        {
                            t.timespan = Convert.ToInt32(t.date);
                            t.fecha = UnixTimeStampToDateTime(t.timespan);
                            t.idMoneda = m.ID;
                            bdd.altaTransaccionBitstamp(t);
                        }
                    }

                }


            }
            catch (Exception ex)
            {

            }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }


        private void guardaValorMonedas()
        {
            try
            {
                foreach (TIPO_MONEDA m in monedas)
                {
                    GetMarketSummaryResponse res = ex.GetMarketSummary(m.moneda);
                    res.TimeStamp = res.TimeStamp.AddHours(2); // PONEMOS HORA ESPAÑOLA                   
                    bdd.altaMoneda(res);

                    //listBox2.Items.Add(res.MarketName + " - HIGH: " + res.High + " LOW: " + res.Low);

                    //if (listBox2.Items.Count > 300)
                    //    listBox2.Items.Clear();

                }

            }
            catch (Exception ex)
            {

            }
        }



        /// <summary>
        /// CARGA HISTORICO A PARTI DE CSV
        /// </summary>
        private void cargaHistorico()
        {
            int contFilas = 0;


            try
            {
                string path = @"C:\Users\juan.roncero\Documents\proyectos\csharp-bittrex-api-master\csharp-bittrex-api-master\consumer\History\bitstampUSD_1-min_data_2012-01-01_to_2017-05-31.csv";


                path = Application.StartupPath + "\\bitstampUSD_1-min_data_2012-01-01_to_2017-05-31.csv";

                // LEEMOS EXCEL

                // CAMPO 1 - TIMESTAMP
                System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

                GetMarketSummaryResponse valor = new GetMarketSummaryResponse();

                using (var fs = File.OpenRead(path))

                using (var reader = new StreamReader(fs))
                {
                    // Quitamos titulos
                    reader.ReadLine();

                    while (!reader.EndOfStream)
                    {
                        // 850413

                        contFilas++;

                        var line = reader.ReadLine();
                        var values = line.Split(',');


                        // 0 1487927400,      TIMESTAMP
                        // 1 1191.01,         OPEN
                        // 2 1192.31,         HIGH
                        // 3 1188.87,         LOW
                        // 4 1188.92,         CLOSE
                        // 5 55.71403603,     VOLUME_(BTC)
                        // 6 66393.72532,     VOLUME_(CURRENCY(
                        // 7 1191.6875899     WEIGHTED_PRICE


                        if (contFilas > 2704343)
                        {

                            // LIMPIAMOS NOT A NUMBER POR 0

                            if (values[7].ToString() != "NaN")
                            {

                                for (int i = 0; i < values.Length; i++)
                                {
                                    if (values[i].ToString() == "NaN")
                                        values[i] = "0";

                                    if (values[i].ToString() == "")
                                        values[i] = "0";
                                }

                                try
                                {
                                    var ci = CultureInfo.InvariantCulture.Clone() as CultureInfo;
                                    ci.NumberFormat.NumberDecimalSeparator = ".";

                                    double tempFecha = Convert.ToDouble(values[0].ToString());
                                    //valor.MarketName = "USDT-BTC-HIST";
                                    valor.MarketName = "USDT-BTC";
                                    valor.TimeStamp = dtDateTime.AddSeconds(tempFecha).ToLocalTime();
                                    valor.Open = Convert.ToDecimal(values[1].ToString());


                                    valor.Bid = decimal.Parse(values[7].ToString().Trim(), ci);

                                    //valor.High = Convert.ToDecimal(values[2].ToString());
                                    valor.High = decimal.Parse(values[2].ToString().Trim(), ci);
                                    //valor.Low = Convert.ToDecimal(values[3].ToString());
                                    valor.Low = decimal.Parse(values[3].ToString().Trim(), ci);
                                    valor.Close = Convert.ToDecimal(values[4].ToString());

                                    if (values[5].ToString().Contains("e"))
                                    {
                                        int ipos = values[5].ToString().IndexOf("e");
                                        string tempVolume = values[5].ToString().Substring(0, ipos);

                                        valor.Volume = 0;
                                    }
                                    else
                                    {
                                        valor.Volume = Convert.ToDecimal(values[5].ToString());
                                    }

                                    if (values[6].ToString().Contains("e"))
                                    {


                                        int ipos = values[6].ToString().IndexOf("e");

                                        string tempVolume = values[6].ToString().Substring(0, ipos);

                                        valor.BaseVolume = 0;

                                    }
                                    else
                                    {
                                        valor.BaseVolume = Convert.ToDecimal(values[6].ToString());
                                    }

                                    //valor.BaseVolume = Convert.ToDecimal(values[6].ToString());

                                    //decimal valorAntiguo = Convert.ToDecimal(values[7].ToString());

                                    valor.Bid = decimal.Parse(values[7].ToString().Trim(), ci); // 1.1



                                    //if (valorAntiguo > 6000)
                                    //{
                                    bdd.altaMonedaHistorico(valor);
                                    //}

                                }
                                catch (Exception ex)
                                {
                                    listBox1.Items.Add(ex.ToString());

                                }




                            }
                        }


                    }
                }

            }
            catch (Exception ex)
            {
                listBox1.Items.Add(ex.ToString());
            }
        }

        private void testRest2()
        {

            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(pepe);

                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                string content = string.Empty;
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        content = sr.ReadToEnd();
                    }
                }

                var releases = JArray.Parse(content);

            }
            catch (Exception ex)
            {


            }
        }

        private void testRest()
        {

            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/restsharp/restsharp/releases");

                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                string content = string.Empty;
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        content = sr.ReadToEnd();
                    }
                }

                var releases = JArray.Parse(content);

            }
            catch (Exception ex)
            {


            }
        }

        public async Task DownloadData(string zip)
        {
            try
            {
                //string url = string.Format("http://search.ams.usda.gov/v1/data.svc/zipSearch?zip={0}", "");


                //HttpClient client = new HttpClient();
                //client.DefaultRequestHeaders.Add("Accept", "application/json");

                //var jsonString = await client.GetStringAsync(zip);

                //JToken token = JToken.Parse(jsonString);

                //foreach (var item in token)
                //{

                //}
            }
            catch (Exception ex)
            {

            }
        }

        #region DESCARGA_CARGA_AUTOMATICA_VALORES

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {

                contSegundos++;

                if (contSegundos == 30)
                {
                    guardaTransaccionesOptimizado2();
                    guardaValorMonedas();
                    contSegundos = 0;
                }

                lblTrans.Text = (30 - contSegundos).ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

            }
        }

        private void inicioCargaAutomatica()
        {



            //timer1.Enabled = true;
            //// 1 minutos
            //timer1.Interval = 60000;

            timer1.Interval = 60 * 1000;
            listBox1.Items.Add("Inicio - " + DateTime.Now.ToString());
            timer1.Enabled = true;

            guardaValorMonedas();


            //listBox1.Items.Add("Carga - " + DateTime.Now.ToString());

        }

        #endregion

        private void timer2_Tick(object sender, EventArgs e)
        {
            contSegundosBallenas++;

            // 10 minutos - 10 * 60  
            if (contSegundosBallenas == 600)
            {
                procesaBallenas();
                contSegundosBallenas = 0;
            }

            lbBallenas.Text = (600 - contSegundosBallenas).ToString();
        }
    }
}

