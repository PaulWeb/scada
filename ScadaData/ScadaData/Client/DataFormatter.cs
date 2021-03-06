﻿/*
 * Copyright 2017 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaData
 * Summary  : Formatter for input channel and event data
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2017
 */

using Scada.Data.Models;
using Scada.Data.Tables;
using System;
using System.Globalization;
using System.Text;

namespace Scada.Client
{
    /// <summary>
    /// Formatter for input channel and event data
    /// <para>Форматирование данных входных каналов и событий</para>
    /// </summary>
    public class DataFormatter
    {
        /// <summary>
        /// Делегат получения свойств статуса входного канала
        /// </summary>
        public delegate CnlStatProps GetCnlStatPropsDelegate(int stat);

        /// <summary>
        /// Пустое значение входного канала
        /// </summary>
        public const string EmptyVal = "---";
        /// <summary>
        /// Отсутствующее значение входного канала
        /// </summary>
        protected const string NoVal = "";
        /// <summary>
        /// Значение входного канала в случае ошибки при форматировании
        /// </summary>
        protected const string FrmtErrVal = "!";
        /// <summary>
        /// Обозначение следующего часа
        /// </summary>
        protected const string NextHourVal = "*";
        /// <summary>
        /// Количество знаков дробной части по умолчанию
        /// </summary>
        protected const int DefDecDig = 3;
        /// <summary>
        /// Цвет значения по умолчанию
        /// </summary>
        protected const string DefColor = "black";
        /// <summary>
        /// Цвет значения "Вкл"
        /// </summary>
        protected const string OnColor = "green";
        /// <summary>
        /// Цвет значения "Откл"
        /// </summary>
        protected const string OffColor = "red";
        /// <summary>
        /// Время отображения текущих данных
        /// </summary>
        protected static readonly TimeSpan CurDataVisibleSpan = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Формат вещественных чисел
        /// </summary>
        protected readonly NumberFormatInfo nfi;
        /// <summary>
        /// Разделитель дробной части по умолчанию
        /// </summary>
        protected readonly string defDecSep;
        /// <summary>
        /// Разделитель групп цифр по умолчанию
        /// </summary>
        protected readonly string defGrSep;


        /// <summary>
        /// Конструктор
        /// </summary>
        public DataFormatter()
            : this(Localization.Culture)
        {
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public DataFormatter(CultureInfo cultureInfo)
        {
            nfi = (NumberFormatInfo)cultureInfo.NumberFormat.Clone();
            defDecSep = nfi.NumberDecimalSeparator;
            defGrSep = nfi.NumberGroupSeparator;
        }


        /// <summary>
        /// Форматировать значение входного канала
        /// </summary>
        public string FormatCnlVal(double val, int stat, InCnlProps cnlProps, bool appendUnit)
        {
            string text;
            string textWithUnit;
            bool textIsNumber;
            FormatCnlVal(val, stat, cnlProps, null, null, out text, out textWithUnit, out textIsNumber);
            return appendUnit ? textWithUnit : text;
        }

        /// <summary>
        /// Форматировать значение входного канала
        /// </summary>
        public void FormatCnlVal(double val, int stat, InCnlProps cnlProps, 
            out string text, out string textWithUnit)
        {
            bool textIsNumber;
            FormatCnlVal(val, stat, cnlProps, null, null, out text, out textWithUnit, out textIsNumber);
        }

        /// <summary>
        /// Форматировать значение входного канала
        /// </summary>
        public void FormatCnlVal(double val, int stat, InCnlProps cnlProps, string decSep, string grSep, 
            out string text, out string textWithUnit, out bool textIsNumber, bool throwOnError = false)
        {
            bool cnlPropsIsNull = cnlProps == null;

            try
            {
                if (stat <= 0)
                {
                    text = textWithUnit = EmptyVal;
                    textIsNumber = false;
                }
                else
                {
                    text = textWithUnit = NoVal;
                    textIsNumber = false;

                    int formatID = cnlPropsIsNull ? 0 : cnlProps.FormatID;
                    int unitArrLen = cnlPropsIsNull || cnlProps.UnitArr == null ? 0 : cnlProps.UnitArr.Length;

                    if (cnlPropsIsNull || cnlProps.ShowNumber)
                    {
                        string unit = unitArrLen > 0 ? " " + cnlProps.UnitArr[0] : "";

                        nfi.NumberDecimalDigits = cnlPropsIsNull ? DefDecDig : cnlProps.DecDigits;
                        nfi.NumberDecimalSeparator = decSep == null ? defDecSep : decSep;
                        nfi.NumberGroupSeparator = grSep == null ? defGrSep : grSep;

                        text = val.ToString("N", nfi);
                        textWithUnit = text + unit;
                        textIsNumber = true;
                    }
                    else if (formatID == BaseValues.Formats.EnumText)
                    {
                        if (unitArrLen > 0)
                        {
                            int unitInd = (int)val;
                            if (unitInd < 0)
                                unitInd = 0;
                            else if (unitInd >= unitArrLen)
                                unitInd = unitArrLen - 1;
                            text = textWithUnit = cnlProps.UnitArr[unitInd];
                        }
                    }
                    else if (formatID == BaseValues.Formats.AsciiText)
                    {
                        byte[] buf = BitConverter.GetBytes(val);
                        text = textWithUnit = Encoding.ASCII.GetString(buf).TrimEnd((char)0);
                    }
                    else if (formatID == BaseValues.Formats.UnicodeText)
                    {
                        byte[] buf = BitConverter.GetBytes(val);
                        text = textWithUnit = Encoding.Unicode.GetString(buf).TrimEnd((char)0);
                    }
                    else if (formatID == BaseValues.Formats.DateTime)
                    {
                        text = textWithUnit = ScadaUtils.DecodeDateTime(val).ToLocalizedString();
                    }
                    else if (formatID == BaseValues.Formats.Date)
                    {
                        text = textWithUnit = ScadaUtils.DecodeDateTime(val).ToLocalizedDateString();
                    }
                    else if (formatID == BaseValues.Formats.Time)
                    {
                        text = textWithUnit = ScadaUtils.DecodeDateTime(val).ToLocalizedTimeString();
                    }
                }
            }
            catch (Exception ex)
            {
                if (throwOnError)
                {
                    string cnlNumStr = cnlPropsIsNull ? "?" : cnlProps.CnlNum.ToString();
                    throw new ScadaException(string.Format(Localization.UseRussian ?
                        "Ошибка при форматировании значения входного канала {0}" :
                        "Error formatting value of input channel {0}", cnlNumStr), ex);
                }
                else
                {
                    text = textWithUnit = FrmtErrVal;
                    textIsNumber = false;
                }
            }
        }

        /// <summary>
        /// Получить текст события
        /// </summary>
        public string GetEventText(EventTableLight.Event ev, InCnlProps cnlProps, CnlStatProps cnlStatProps)
        {
            if (string.IsNullOrEmpty(ev.Descr))
            {
                // текст в формате "<статус>: <значение>"
                StringBuilder sbText = cnlStatProps == null ?
                    new StringBuilder() : new StringBuilder(cnlStatProps.Name);

                if (ev.NewCnlStat > BaseValues.CnlStatuses.Undefined)
                {
                    if (sbText.Length > 0)
                        sbText.Append(": ");
                    sbText.Append(FormatCnlVal(ev.NewCnlVal, ev.NewCnlStat, cnlProps, true));
                }

                return sbText.ToString();
            }
            else
            {
                // только описание события
                return ev.Descr;
            }
        }

        /// <summary>
        /// Получить цвет значения входного канала
        /// </summary>
        public string GetCnlValColor(double val, int stat, InCnlProps cnlProps, CnlStatProps cnlStatProps)
        {
            try
            {
                if (cnlProps == null)
                {
                    return DefColor;
                }
                else
                {
                    if (cnlProps.ShowNumber ||
                        cnlProps.UnitArr == null || cnlProps.UnitArr.Length != 2 ||
                        stat == BaseValues.CnlStatuses.Undefined ||
                        stat == BaseValues.CnlStatuses.FormulaError ||
                        stat == BaseValues.CnlStatuses.Unreliable)
                    {
                        return cnlStatProps == null || string.IsNullOrEmpty(cnlStatProps.Color) ? 
                            DefColor : cnlStatProps.Color;
                    }
                    else
                    {
                        return val > 0 ? OnColor : OffColor;
                    }
                }
            }
            catch (Exception ex)
            {
                string cnlNumStr = cnlProps == null ? cnlProps.CnlNum.ToString() : "?";
                throw new ScadaException(string.Format(Localization.UseRussian ?
                    "Ошибка при получении цвета значения входного канала {0}" :
                    "Error getting color of input channel {0}", cnlNumStr), ex);
            }
        }

        /// <summary>
        /// Определить необходимость отображения текущих данных
        /// </summary>
        public bool CurDataVisible(DateTime dataAge, DateTime nowDT, out string emptyVal)
        {
            emptyVal = NoVal;
            return nowDT - dataAge <= CurDataVisibleSpan;
        }

        /// <summary>
        /// Определить необходимость отображения часовых данных
        /// </summary>
        public bool HourDataVisible(DateTime dataAge, DateTime nowDT, bool snapshotExists, out string emptyVal)
        {
            if (snapshotExists || dataAge.Date < nowDT.Date)
            {
                emptyVal = EmptyVal;
                return snapshotExists;
            }
            else if (dataAge.Date > nowDT.Date)
            {
                emptyVal = NoVal;
                return false;
            }
            else // dataAge.Date == nowDT.Date
            {
                if (dataAge.Hour > nowDT.Hour + 1)
                {
                    emptyVal = NoVal;
                    return false;
                }
                else if (dataAge.Hour == nowDT.Hour + 1)
                {
                    emptyVal = NextHourVal;
                    return false;
                }
                else
                {
                    emptyVal = EmptyVal;
                    return snapshotExists;
                }
            }
        }
    }
}
