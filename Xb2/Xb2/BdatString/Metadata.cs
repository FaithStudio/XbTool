﻿using System;
using System.Linq;
using System.Text;
using Xb2.Bdat;
using Xb2.Types;

namespace Xb2.BdatString
{
    public static class Metadata
    {
        public static void ApplyMetadata(BdatStringCollection tables)
        {
            foreach ((string table, string member) reference in tables.Bdats.BdatFields.Keys)
            {
                BdatStringTable table = tables[reference.table];

                foreach (BdatStringItem item in table.Items)
                {
                    ResolveItemRef(item[reference.member]);
                }
            }
        }

        public static void ResolveItemRef(BdatStringValue value)
        {
            if (value.Resolved) return;

            BdatStringItem item = value.Parent;
            BdatStringTable table = item.Table;
            BdatStringCollection tables = table.Collection;
            BdatMember member = value.Member;
            BdatFieldInfo field = member.Metadata;

            if (field == null)
            {
                value.Resolved = true;
                return;
            }

            int refId = int.Parse(value.ValueString) + field.Adjust;

            switch (field.Type)
            {
                case BdatFieldType.Message:
                    value.Display = tables[field.RefTable][refId]?["name"].DisplayString;
                    if (string.IsNullOrWhiteSpace(value.DisplayString) && refId > 0)
                    {
                        value.Display = refId.ToString();
                    }
                    break;
                case BdatFieldType.Reference:
                    ApplyRef(field.RefTable);
                    break;
                case BdatFieldType.Item:
                    ApplyRef(BdatStringTools.GetItemTable(refId));
                    break;
                case BdatFieldType.Event:
                    ApplyRef(BdatStringTools.GetEventTable(refId));
                    break;
                case BdatFieldType.QuestFlag:
                    ApplyRef(BdatStringTools.GetQuestListTable(refId));
                    break;
                case BdatFieldType.Condition:
                    var conditionType = (ConditionType)int.Parse(item[field.RefField].ValueString);
                    ApplyRef(BdatStringTools.GetConditionTable(conditionType));
                    break;
                case BdatFieldType.Task:
                    var taskType = (TaskType)int.Parse(item[field.RefField].ValueString);
                    ApplyRef(BdatStringTools.GetTaskTable(taskType));
                    break;
                case BdatFieldType.ShopTable:
                    var shopType = (ShopType)int.Parse(item[field.RefField].ValueString);
                    ApplyRef(BdatStringTools.GetShopTable(shopType));
                    break;
                case BdatFieldType.Character:
                    ApplyRef(BdatStringTools.GetCharacterTable(refId));
                    break;
                case BdatFieldType.Enhance:
                    value.Display = BdatStringTools.GetEnhanceCaption(value);
                    break;
                case BdatFieldType.WeatherIdMap:
                    value.Display = BdatStringTools.PrintWeatherIdMap(refId, 13, tables);
                    break;
                case BdatFieldType.PouchBuff:
                    value.Display = GetPouchBuffCaption(value);
                    break;
            }

            if (field.EnumType != null)
            {
                if (field.EnumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0)
                {
                    value.Display = BdatStringTools.PrintEnumFlags(field.EnumType, refId);
                }
                else
                {
                    value.Display = Enum.GetName(field.EnumType, refId);
                }
            }

            value.Resolved = true;

            void ApplyRef(string refTable)
            {
                if (refTable == null || !tables[refTable].ContainsId(refId))
                {
                    value.Display = refId == 0 ? null : refId.ToString();
                    return;
                }

                var reft = tables[refTable][refId].Display;

                if (reft != null)
                {
                    if (!reft.Resolved)
                    {
                        ResolveItemRef(reft);
                    }

                    if (!string.IsNullOrWhiteSpace(reft.DisplayString))
                    {
                        value.Display = reft.Display;
                    }
                }

                value.Reference = tables[refTable][refId];
                tables[refTable][refId].ReferencedBy.Add(value.Parent);
            }
        }

        public static string GetPouchBuffCaption(BdatStringValue value)
        {
            if (value == null) return null;

            var item = value.Parent;
            var field = value.Member.Metadata;
            var tables = item.Table.Collection;

            int captionId = int.Parse(value.ValueString);
            BdatStringValue captionValue = tables["BTL_PouchBuff"][captionId]?["Name"];

            if (captionValue == null) return null;

            if (!captionValue.Resolved)
            {
                ResolveItemRef(captionValue);
            }

            string caption = captionValue.DisplayString;
            if (caption == null) return null;

            var sb = new StringBuilder(caption);

            var tags = BdatStringTools.ParseTags(caption);

            foreach (var tag in tags.OrderByDescending(x => x.Start))
            {
                if (tag.SubType != "PouchParam") continue;

                float buffValue = float.Parse(item[field.RefField].ValueString);

                sb.Remove(tag.Start, tag.Length);
                sb.Insert(tag.Start, buffValue);
            }

            return sb.ToString();
        }
    }
}