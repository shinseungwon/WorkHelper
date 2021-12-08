﻿using iTextSharp.text.pdf;
using System;
using HelperDotNet;
using System.IO;
using iTextSharp.text;
using System.Data;
using System.Diagnostics;

namespace PickslipPrinterV3
{
    class Program
    {
        static void Main(string[] args)
        {
            //1.Get Config
            ConfigHelper ch = new ConfigHelper("Config.txt");

            string id = "jeffrey";
            string password = "SUNGwon@530";
            //string id = ch.Configs["id"];
            //string password = ch.Configs["password"];

            //string ip = "172.22.8.143";
            //string catalog = "krlocal";
            string ip = ch.Configs["ip"];
            string catalog = ch.Configs["catalog"];

            //string brand = "HM";
            //string stlkey = "0000344567";
            //string endkey = "0000344567";
            string brand = ch.Configs["brand"];
            string stlkey = ch.Configs["stlkey"];
            string endkey = ch.Configs["endkey"];

            //2.Connect DB + Call SP
            string connectionString = "Data Source=" + ip + ",1433; Initial Catalog=" + catalog
                + "; User id=" + id + "; Password=" + password + ";";

            string query = "exec krlocal..isp_kr_hmpickslip02 '"
                + brand + "', '99', '" + stlkey + "', '" + endkey + "'";

            DbHelper dh = new DbHelper(connectionString);
            Logger l = new Logger(Directory.GetCurrentDirectory() + @"\Logger");
            dh.SetLogger(l);
            DataSet ds = new DataSet();
            dh.CallQuery(query, ref ds);

            //3. Create File
            if (!Directory.Exists("Reports"))
            {
                Directory.CreateDirectory("Reports");
            }

            string filename = @"Reports\Pickslip_" + brand + "_" + stlkey + "~" + endkey + ".pdf";
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            Document doc = new Document(new Rectangle(200, 130), 5, 5, 5, 5);
            PdfWriter writer = PdfWriter.GetInstance(doc, fs);
            doc.Open();
                        
            FontFactory.Register("malgun.ttf");
            FontFactory.Register("malgunsl.ttf");
            FontFactory.Register("malgunbd.ttf");

            foreach (DataRow r in ds.Tables[0].Rows)
            {
                //Frame Rects
                PdfContentByte cb1 = writer.DirectContent;
                cb1.Rectangle(5, 5, 190, 120);
                cb1.Stroke();

                PdfContentByte cb2 = writer.DirectContent;
                cb2.Rectangle(150, 90, 45, 35);
                cb2.FillStroke();

                //OrderNo
                Font orderNoFont = FontFactory.GetFont("맑은 고딕", BaseFont.IDENTITY_H, 14, Font.BOLD, BaseColor.WHITE);
                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, new Phrase("HM", orderNoFont), 170, 110, 0);
                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, new Phrase(r["OrderNo"].ToString(), orderNoFont), 170, 95, 0);

                //Location
                Font locFont = FontFactory.GetFont("맑은 고딕", BaseFont.IDENTITY_H, 18, Font.BOLD, BaseColor.BLACK);
                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, new Phrase("Loc: " + r["Loc"], locFont), 75, 100, 0);

                //Sku
                Font skuFont = FontFactory.GetFont("맑은 고딕", BaseFont.IDENTITY_H, 16, Font.BOLD, BaseColor.BLACK);
                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, new Phrase("SKU: " + r["SKU"], skuFont), 85, 75, 0);

                //Color
                Font colorFont = FontFactory.GetFont("맑은 고딕", BaseFont.IDENTITY_H, 16, Font.BOLD, BaseColor.BLACK);
                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, new Phrase(r["Color"].ToString(), colorFont), 177, 74, 0);

                //Qty
                Font qtyFont = FontFactory.GetFont("맑은 고딕", BaseFont.IDENTITY_H, 16, Font.BOLD, BaseColor.BLACK);
                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, new Phrase("Qty: " + r["QTY"], qtyFont), 170, 60, 0);

                //LoadKey
                Font loadFont1 = FontFactory.GetFont("맑은 고딕", BaseFont.IDENTITY_H, 12, Font.NORMAL, BaseColor.BLACK);
                Font loadFont2 = FontFactory.GetFont("맑은 고딕", BaseFont.IDENTITY_H, 12, Font.UNDERLINE, BaseColor.BLACK);
                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, new Phrase("Load: ", loadFont1), 30, 63, 0);
                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, new Phrase(r["LoadKey"].ToString(), loadFont2), 80, 63, 0);

                //OrderKey
                Font orderFont = FontFactory.GetFont("맑은 고딕", BaseFont.IDENTITY_H, 12, Font.NORMAL, BaseColor.BLACK);
                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, new Phrase("Order: " + r["OrderKey"], orderFont), 62, 49, 0);

                //Sequence
                Font seqFont = FontFactory.GetFont("맑은 고딕", BaseFont.IDENTITY_H, 10, Font.NORMAL, BaseColor.BLACK);
                ColumnText.ShowTextAligned(writer.DirectContent, Element.ALIGN_CENTER, new Phrase(r["LineNumber"] + "/" + r["ExternLineNo"] + "/" + r["TotalLines"], orderFont), 100, 10, 0);

                //Barcode
                Barcode128 barcodeImg = new Barcode128();
                barcodeImg.Code = r["OrderKey"].ToString();
                System.Drawing.Image img = barcodeImg.CreateDrawingImage(System.Drawing.Color.Black, System.Drawing.Color.White);
                iTextSharp.text.Image pic = iTextSharp.text.Image.GetInstance(img, System.Drawing.Imaging.ImageFormat.Jpeg);
                pic.SetAbsolutePosition(55, 20);
                doc.Add(pic);

                doc.NewPage();
            }

            doc.Close();

            //4. Open File
            Process.Start(filename);
        }
    }
}
