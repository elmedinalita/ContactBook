using ContactBook.Models;
using System.Text;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Data;

namespace ContactBook
{
    public class Helper
    {
        // Import
        public static List<Contact> ImportCSV(ref string data)
        {
            List<Contact> contacts = new List<Contact>();
            var lines = data.Split(Environment.NewLine);
            foreach (var line in lines)
            {
                // First Name,Last Name,Email,Phone,Address,
                var contact = new Contact();
                contact.FirstName = line.Split(',')[0];
                contact.LastName = line.Split(',')[1];
                contact.Email = StringToEmails(line.Split(',')[2]);
                contact.Phone = StringToPhones(line.Split(',')[2]);
                contact.Address = StringToAddresses(line.Split(',')[2]);

                contacts.Add(contact);
            }
            return contacts;
        }
        public static List<Contact> ImportExcel(string filePath)
        {
            List<Contact> contacts = new List<Contact>();

            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false))
            {
                WorkbookPart workbookPart = doc.WorkbookPart!;
                SharedStringTablePart sstpart = workbookPart.GetPartsOfType<SharedStringTablePart>().First();
                SharedStringTable sst = sstpart.SharedStringTable;

                WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                Worksheet sheet = worksheetPart.Worksheet;

                var rows = sheet.Descendants<Row>();

                foreach (Row row in rows)
                {
                    var elements = row.Elements<Cell>().ToArray();

                    // First Name,Last Name,Email,Phone,Address,
                    var contact = new Contact();
                    contact.FirstName = elements[0].CellValue!.Text;
                    contact.LastName = elements[1].CellValue!.Text;
                    if (string.IsNullOrWhiteSpace(contact.FirstName) || string.IsNullOrWhiteSpace(contact.LastName))
                        continue; // Skip

                    contact.Email = StringToEmails(elements[2].CellValue!.Text);
                    contact.Phone = StringToPhones(elements[3].CellValue!.Text);
                    contact.Address = StringToAddresses(elements[4].CellValue!.Text);

                    foreach (Cell c in row.Elements<Cell>())
                    {
                        if (c.CellValue == null)
                            continue;
                        Console.WriteLine("Cell contents: {0}", c.CellValue.Text);
                    }
                }
            }
            return contacts;
        }

        static List<Email> StringToEmails(string str)
        {
            List<Email> list = new List<Email>();
            foreach (var item in str.Split(','))
            {
                if (item.Trim() != "")
                    list.Add(new Email { EmailAddr = item });
            }
            return list;
        }
        static List<Phone> StringToPhones(string str)
        {
            List<Phone> list = new List<Phone>();
            foreach (var item in str.Split(','))
            {
                if (item.Trim() != "")
                    list.Add(new Phone { PhoneNo = item });
            }
            return list;
        }
        static List<Address> StringToAddresses(string str)
        {
            List<Address> list = new List<Address>();
            foreach (var item in str.Split(','))
            {
                if (item.Trim() != "")
                    list.Add(new Address { Addr = item });
            }
            return list;
        }

        // Export
        public static string ExportCSV(ref Contact[] contacts)
        {
            List<object> _contacts = new List<object>();
            foreach (var c in contacts)
            {
                if (c.FirstName == "" || c.LastName == "")
                    continue; // Skip empty
                var item = new[]
                {
                    c.FirstName,
                    c.LastName,
                    EmailsToString(c.Email),
                    PhonesToString(c.Phone),
                    AddressesToString(c.Address)
                };
                _contacts.Add(item);
            }
            //Insert the Column Names.
            _contacts.Insert(0, new string[] { "First Name", "Last Name", "Email", "Phone", "Address" });

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _contacts.Count; i++)
            {
                string[] con = (string[])_contacts[i];
                for (int j = 0; j < con.Length; j++)
                {
                    //Append data with separator.
                    sb.Append(con[j] + ',');
                }
                //Append new line character.
                sb.Append("\r\n");
            }
            sb.Remove(sb.Length - 3, 2);
            return sb.ToString();
        }
        public static void ExportExcel(string _filePath, ref Contact[] contacts)
        {
            // Lets converts our object data to Datatable for a simplified logic.
            // Datatable is most easy way to deal with complex datatypes for easy reading and formatting. 
            DataTable table = (DataTable)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Helper.ConvertContactList(contacts)), typeof(DataTable))!;

            using SpreadsheetDocument document = SpreadsheetDocument.Create(_filePath, SpreadsheetDocumentType.Workbook);
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" };

            sheets.Append(sheet);

            Row headerRow = new Row();

            List<string> columns = new List<string>();
            foreach (DataColumn column in table!.Columns)
            {
                columns.Add(column.ColumnName);

                Cell cell = new Cell();
                cell.DataType = CellValues.String;
                cell.CellValue = new CellValue(column.ColumnName);
                headerRow.AppendChild(cell);
            }

            sheetData.AppendChild(headerRow);

            foreach (DataRow dsrow in table.Rows)
            {
                Row newRow = new Row();
                foreach (string col in columns)
                {
                    Cell cell = new Cell();
                    cell.DataType = CellValues.String;
                    cell.CellValue = new CellValue(dsrow[col].ToString()!);
                    newRow.AppendChild(cell);
                }

                sheetData.AppendChild(newRow);
            }

            workbookPart.Workbook.Save();
        }
        public static List<ManagableContact> ConvertContactList(Contact[] contacts)
        {
            var managableContacts = new List<ManagableContact>();
            foreach (var contact in contacts)
            {
                managableContacts.Add(new ManagableContact()
                {
                    Id = contact.Id,
                    FirstName = contact.FirstName,
                    LastName = contact.LastName,
                    Email = EmailsToString(contact.Email),
                    Phone = PhonesToString(contact.Phone),
                    Address = AddressesToString(contact.Address)
                });
            }
            return managableContacts;
        }

        static string EmailsToString(List<Email> emails)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var em in emails)
                sb.Append(em.EmailAddr + ";");
            sb.Remove(sb.Length - 1, 1); // Remove the last ;
            return sb.ToString();
        }
        static string PhonesToString(List<Phone> phones)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ph in phones)
                sb.Append(ph.PhoneNo + ";");
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
        static string AddressesToString(List<Address> addresses)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var add in addresses)
                sb.Append(add.Addr + ";");
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
    }
}
