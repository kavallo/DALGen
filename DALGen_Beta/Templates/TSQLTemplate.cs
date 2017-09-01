﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DALGen_Beta
{
    class TSQLTemplate : IDALTemplate
    {
        public void GenerateContent(DALEntity entity, String outputFilePath)
        {
            /****************************
            // Create File
            ****************************/
            string filename = "SQL_" + entity.EntityName;
            string textBuffer = String.Empty;
            string tempSprocName = String.Empty;
            string tempParamName = String.Empty;
            int count = 0;

            string path = String.Format(@"{0}\\{1}.sql", outputFilePath, filename);
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                /****************************
                // Initial Comments
                ****************************/
                
                textBuffer = "/*\n";
                textBuffer += "Author:\t\t\tThis code was generated by DALGen version 1.0.0.0\n";
                textBuffer += "Date:\t\t\t" + DateTime.Now.ToShortDateString() + "\n";
                textBuffer += "Description:\tCreates the " + entity.EntityName + " table and respective stored procedures\n";
                textBuffer += "\n"; 
                textBuffer += "*/\n";
                sw.WriteLine(textBuffer);

                /****************************
                // Generate DB Schema
                ****************************/
                // Create Table
                textBuffer = "\n";
                textBuffer += "--------------------------------------------------------------\n";
                textBuffer += "-- Create table\n";
                textBuffer += "--------------------------------------------------------------\n";
                textBuffer += "\n";
                sw.WriteLine(textBuffer);

                textBuffer = "\n";
                textBuffer += "CREATE TABLE " + entity.EntityName + " (\n";
                count = 0;
                foreach (var attribute in entity.Attributes)
                {
                    textBuffer += attribute.AttributeName + " " + GetTSQLDataTypeString(attribute.DataType, attribute.AttributeSize);
                    textBuffer += (attribute.IsPrimaryKey) ? " IDENTITY(1,1)" : "";
                    if (++count < entity.Attributes.Count)
                        textBuffer += ",";
                    textBuffer += "\n";
                }

                // Add contraints
                foreach (var attribute in entity.Attributes.Where(x => x.IsPrimaryKey || x.IsForeignKey).ToList())
                {
                    if (attribute.IsPrimaryKey)
                    {
                        textBuffer += "CONSTRAINT pk_" + entity.EntityName + "_" + attribute.AttributeName 
                                   + " PRIMARY KEY (" + attribute.AttributeName + ")\n";
                    }
                    else if (attribute.IsForeignKey)
                    {
                        textBuffer += "CONSTRAINT fk_" + entity.EntityName + "_" + attribute.AttributeName + "_" + attribute.ReferenceEntity + "_" 
                                   + attribute.ReferenceAttribute + " FOREIGN KEY (" + attribute.AttributeName + ") REFERENCES " 
                                   + attribute.ReferenceEntity + " (" + attribute.ReferenceAttribute + ")\n";
                    }
                }

                textBuffer += ");\n";
                textBuffer += "\n";
                sw.WriteLine(textBuffer);

                /****************************
                // Generate Sprocs
                ****************************/
                textBuffer = "\n";
                textBuffer += "--------------------------------------------------------------\n";
                textBuffer += "-- Create default SCRUD sprocs for this table\n";
                textBuffer += "--------------------------------------------------------------\n";
                textBuffer += "\n";
                sw.WriteLine(textBuffer);

                // Get
                tempSprocName = "usp_" + entity.EntityName + "_Get";
                textBuffer = "\n";
                textBuffer += "IF EXISTS (SELECT * FROM SYSOBJECTS WHERE ID = OBJECT_ID('" + tempSprocName + "') AND sysstat & 0xf = 4)\n";
                textBuffer += "\tDROP PROCEDURE [dbo].[" + tempSprocName + "];\n";
                textBuffer += "GO\n";
                textBuffer += "\n";
                sw.WriteLine(textBuffer);

                textBuffer = "\n";
                textBuffer += "CREATE PROCEDURE [dbo].[" + tempSprocName + "]\n";
                textBuffer += "(\n";

                count = 0;
                foreach(var pk in entity.Attributes.Where(x => x.IsPrimaryKey).ToList())
                {
                    tempParamName = "@param" + pk.AttributeName + " " + GetTSQLDataTypeString(pk.DataType, pk.AttributeSize);
                    textBuffer += "\t" + tempParamName;
                    if (++count < entity.Attributes.Where(x => x.IsPrimaryKey).ToList().Count)
                        textBuffer += ",";
                    textBuffer += "\n";
                }

                textBuffer += ")\n";
                textBuffer += "AS\n";
                textBuffer += "BEGIN\n";
                textBuffer += "\tSET NOCOUNT ON\n";
                textBuffer += "\tDECLARE @Err INT\n\n";
                textBuffer += "\tSELECT\n";

                count = 0;
                foreach(var attribute in entity.Attributes)
                {
                    textBuffer += "\t\t" + entity.EntityName + ".[" + attribute.AttributeName + "] AS " + "[" + attribute.AttributeName + "]";
                    if (++count < entity.Attributes.Count)
                        textBuffer += ",";
                    textBuffer += "\n";
                }
                textBuffer += "\tFROM " + entity.EntityName + "\n";
                textBuffer += "\tWHERE\n";

                count = 0;
                foreach (var pk in entity.Attributes.Where(x => x.IsPrimaryKey).ToList())
                {
                    if (count++ != 0)
                        textBuffer += "\t\tAND ";
                    else
                        textBuffer += "\t\t";

                    tempParamName = "@param" + pk.AttributeName;
                    textBuffer += entity.EntityName + ".[" + pk.AttributeName + "] = " + tempParamName + "\n";
                }

                textBuffer += "\n\tSET @Err = @@Error\n";
                textBuffer += "\n\tRETURN @Err\n";
                textBuffer += "END\n";
                textBuffer += "GO\n\n";
                sw.WriteLine(textBuffer);


                // GetAll
                // GetList
                // Add
                // Update
                // Delete
                // Search

                sw.Close();
            }
        }

        public String GetTSQLDataTypeString(DataType dt, int? dtsize)
        {
            String returnVal = "UNKNOWN";
            switch (dt)
            {
                case DataType.BIT:
                    returnVal = "BIT";
                    break;
                case DataType.CHAR:
                    returnVal = "CHAR";
                    break;
                case DataType.DATE:
                    returnVal = "DATE";
                    break;
                case DataType.DATETIME:
                    returnVal = "DATETIME";
                    break;
                case DataType.DATETIME2:
                    returnVal = "DATETIME2";
                    break;
                case DataType.INT:
                    returnVal = "INT";
                    break;
                case DataType.NCHAR:
                    returnVal = "NCHAR";
                    break;
                case DataType.NVARCHAR:
                    returnVal = "NVARCHAR";
                    break;
                case DataType.VARCHAR:
                    returnVal = "VARCHAR";
                    break;
            }

            if (dtsize != null && dtsize > 0)
                returnVal += "(" + dtsize + ")";

            return returnVal;
        }
    }
}
