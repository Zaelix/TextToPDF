﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TTPDF
{
    class PDFMaker
    {
        static int page_Count = 1;
        static int obj_Count = 1;
        static Page[] pages = new Page[20];
        static string[] fonts = new string[10];
        //files
        private string outputStreamPath = App.outputDirectory + @"\" + App.outputFileName;
        public FileStream outFileStream;

        //string fileStart = "%PDF-1.4\r\n%%EOF\r\n";
        public static string pagesRefObj = "";
        public static string resourceRefObj = "";

        public int Write(string filePath)
        {
            StreamReader sr;
            try
            {
                sr = new StreamReader(filePath);
            }
            catch (Exception)
            {
                Environment.Exit(1);
                throw;
            }
            outFileStream = new FileStream(outputStreamPath, FileMode.Create, FileAccess.Write);

            //Begin the PDF file
            FileStreamWrite(outFileStream, "%PDF-1.4\r\n");         

            //Create font objects.
            FileStreamWrite(outFileStream, CreateFontObject());
            FileStreamWrite(outFileStream, "\r\n");

            //Create resource objects with the fonts.
            FileStreamWrite(outFileStream, CreateResourceObject());
            FileStreamWrite(outFileStream, "\r\n");

            //Create text content objects and the containing Page class instances.
            pages[0] = new Page(480, 640);
            FileStreamWrite(outFileStream, pages[0].StartContentObj());
            string strLine = string.Empty;
            int height = 640;
            while (sr.Peek() >= 0)
            {
                strLine = sr.ReadLine();
                if (strLine.Contains("@")) {
                    FileStreamWrite(outFileStream, pages[page_Count-1].EndContentObj());
                    FileStreamWrite(outFileStream, "\r\n");
                    IncrementPageCount();
                    pages[page_Count-1] = new Page(480, 640);
                    FileStreamWrite(outFileStream, pages[page_Count-1].StartContentObj());
                    strLine = strLine.Replace("@", "");
                    height = 640;
                }
                if (strLine.Contains(@"<<")){
                    strLine = pages[page_Count-1].ChangeFontSize(strLine);
                }
                height = height - pages[page_Count - 1].GetFontSize();
                FileStreamWrite(outFileStream, pages[page_Count - 1].InsertContentLine(strLine, height));
            }
            FileStreamWrite(outFileStream, pages[page_Count-1].EndContentObj());
            FileStreamWrite(outFileStream, "\r\n");

            //Create Pages object from Page class instances found during text content creation. Update Page objects to have Pages object ID
            FileStreamWrite(outFileStream, CreatePagesObject());
            FileStreamWrite(outFileStream, "\r\n");

            //Create Page objects to reference Pages objects.
            foreach (Page p in pages) {
                if (p != null){
                    FileStreamWrite(outFileStream, p.CreatePageObject());
                    FileStreamWrite(outFileStream, "\r\n");
                }
            }

            //Create Catalog object to reference Pages object.
            FileStreamWrite(outFileStream, CreateCatalogObject());

            //End the PDF file
            FileStreamWrite(outFileStream, @"%%EOF");               
            outFileStream.Close();

            return 0;
        }
        //Writes the string on the end of the output file.
        private void FileStreamWrite(FileStream outFileStream, string str1)
        {
            Byte[] buffer = null;
            buffer = ASCIIEncoding.ASCII.GetBytes(str1);
            outFileStream.Write(buffer, 0, buffer.Length);

        }
        public static string CreateResourceObject() {
            string objContent = PDFMaker.GetObjCount() + " 0 obj\r\n<<\r\n/ProcSet[/PDF/Text]\r\n/Font <</F1 " + fonts[0] + " >>\r\n>>\r\nendobj\r\n";
            resourceRefObj = PDFMaker.GetObjCount() + " 0 R";
            PDFMaker.IncrementObjCount();
            return objContent;
        }
        public static string CreateFontObject() {
            int fontID = PDFMaker.GetObjCount();
            string objContent = fontID + " 0 obj\r\n<<\r\n/Type /Font\r\n/Subtype /Type1\r\n/Name /F1\r\n/BaseFont /Helvetica\r\n>>\r\nendobj\r\n";
            PDFMaker.IncrementObjCount();
            fonts[0] = fontID + " 0 R";
            return objContent;
        }
        public static string CreatePagesObject() {
            int obj_ID = PDFMaker.GetObjCount();
            PDFMaker.pagesRefObj = obj_ID + " 0 R";
            PDFMaker.IncrementObjCount();
            string obj = obj_ID + " 0 obj\r\n<<\r\n/Type /Pages\r\n/Kids [ ";
            foreach (Page p in pages) {
                if (p != null){
                    obj = obj + p.GetID() + " 0 R ";
                    p.SetParentRefObj(obj_ID + " 0 R");
                }
            }
            obj = obj + "]\r\n/Count " + PDFMaker.GetPageCount() + "\r\n>>\r\nendobj\r\n";
            return obj;
        }
        private static string CreateCatalogObject() {
            int obj_ID = PDFMaker.GetObjCount();
            PDFMaker.IncrementObjCount();
            string obj = obj_ID + " 0 obj\r\n<<\r\n/Type /Catalog\r\n/Pages " + pagesRefObj + "\r\n>>\r\nendobj\r\n";
            return obj;
        }
        public static void IncrementObjCount(){
            obj_Count++;
        }
        public static int GetObjCount() {
            return obj_Count;
        }
        public static void IncrementPageCount(){
            page_Count++;
        }
        public static int GetPageCount(){
            return page_Count;
        }
    }
    public class Page {
        int obj_ID;
        int fontSize = 12;
        int width;
        int height;
        string parentRefObj;
        string contentRefObj;
        string content;
       
        public Page(int width, int height) {
            this.width = width;
            this.height = height;
            this.obj_ID = PDFMaker.GetObjCount();
            PDFMaker.IncrementObjCount();
        }
        public string CreatePageObject() {
            string pageObj = obj_ID + " 0 obj\r\n<<\r\n/Type /Page\r\n/Parent " + parentRefObj + "\r\n/MediaBox [ 0 0 " + width + " " + height + " ]\r\n/Resources " + PDFMaker.resourceRefObj + "\r\n/Contents " + contentRefObj + "\r\n>>\r\nendobj\r\n";
            return pageObj;
        }
        public string StartContentObj(){
            contentRefObj = PDFMaker.GetObjCount() + " 0 R";
            string objDeclaration = PDFMaker.GetObjCount() + " 0 obj\r\n<<\r\n/Length 53\r\n>>\r\nstream\r\nBT\r\n";
            PDFMaker.IncrementObjCount();
            return objDeclaration;
        }
        public string EndContentObj(){
            string objCloser = "ET\r\nendstream\r\nendobj\r\n";
            return objCloser;
        }
        public string InsertContentLine(string line, int yHeight) {
            int indentPixels = 20;                   //0 = Left edge of the page
            double fontWidth = 1;                   //Scale Multiplier. 1 = Normal size
            double fontHeight = 1;                  //Scale Multiplier. 1 = Normal size
            double italics = 0;                     //Multiplier. 0 = No italics, 1 = EXTREME italics
            string setup = "/F1 " + fontSize + " Tf\r\n" + fontWidth + " 0 " + italics + " " + fontHeight + " " + indentPixels + " " + yHeight + " Tm\r\n";
            string lineContent = "(" + line + ")Tj\r\n";
            return setup + lineContent;
        }
        public void SetContent(string cont) {
            this.content = cont;
        }
        public void SetParentRefObj(string pRO) {
            this.parentRefObj = pRO;
        }
        public string ChangeFontSize(string fsTagLine)
        {
            int i = fsTagLine.IndexOf("<<");
            int j = fsTagLine.IndexOf(">>");
            string fsS = fsTagLine.Substring(i + 2, j - 2);
            int newFS = int.Parse(fsS);
            fsTagLine = fsTagLine.Remove(i, 4 + fsS.Length);
            fontSize = newFS;
            return fsTagLine;
        }
        public int GetFontSize() {
            return fontSize;
        }
        public int GetID() {
            return obj_ID;
        }
    }
}
