﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using Orchard.Utility;

namespace Orchard.Mvc.Html {
    public static class HtmlHelperExtensions {
        public static string NameOf<T>(this HtmlHelper<T> html, Expression<Action<T>> expression) {
            return Reflect.NameOf(html.ViewData.Model, expression);
        }

        public static string NameOf<T, TResult>(this HtmlHelper<T> html, Expression<Func<T, TResult>> expression) {
            return Reflect.NameOf(html.ViewData.Model, expression);
        }

        public static MvcHtmlString SelectOption<T>(this HtmlHelper html, T currentValue, T optionValue, string text) {
            return SelectOption(html, optionValue, object.Equals(optionValue, currentValue), text);
        }

        public static MvcHtmlString SelectOption(this HtmlHelper html, object optionValue, bool selected, string text) {
            var builder = new TagBuilder("option");

            if (optionValue != null)
                builder.MergeAttribute("value", optionValue.ToString());

            if (selected)
                builder.MergeAttribute("selected", "selected");

            builder.SetInnerText(text);

            return MvcHtmlString.Create(builder.ToString(TagRenderMode.Normal));
        }

        #region UnorderedList

        public static string UnorderedList<T>(this HtmlHelper htmlHelper, IEnumerable<T> items, Func<T, int, string> generateContent, string cssClass) {
            return htmlHelper.UnorderedList(items, generateContent, cssClass, null, (string)null);
        }

        public static string UnorderedList<T>(this HtmlHelper htmlHelper, IEnumerable<T> items, Func<T, int, string> generateContent, string cssClass, string itemCssClass, string alternatingItemCssClass) {
            return UnorderedList(items, generateContent, cssClass, t => itemCssClass, t => alternatingItemCssClass);
        }

        private static string UnorderedList<T>(IEnumerable<T> items, Func<T, int, string> generateContent, string cssClass, Func<T, string> generateItemCssClass, Func<T, string> generateAlternatingItemCssClass) {
            if (items == null || items.Count() == 0) return "";

            var sb = new StringBuilder(250);
            int counter = 0, count = items.Count() - 1;

            sb.AppendFormat(
                !string.IsNullOrEmpty(cssClass) ? "<ul class=\"{0}\">" : "<ul>",
                cssClass
                );

            foreach (var item in items) {
                var sbClass = new StringBuilder(50);

                if (counter == 0)
                    sbClass.Append("first ");
                if (counter == count)
                    sbClass.Append("last ");
                if (generateItemCssClass != null)
                    sbClass.AppendFormat("{0} ", generateItemCssClass(item));
                if (counter % 2 != 0 && generateAlternatingItemCssClass != null)
                    sbClass.AppendFormat("{0} ", generateAlternatingItemCssClass(item));

                sb.AppendFormat(
                    sbClass.Length > 0
                        ? string.Format("<li class=\"{0}\">{{0}}</li>", sbClass.ToString().TrimEnd())
                        : "<li>{0}</li>",
                    generateContent(item, counter)
                    );

                counter++;
            }

            sb.Append("</ul>");

            return sb.ToString();
        }

        #endregion

        #region Format Date/Time

        //TODO: (erikpo) This method needs localized
        public static string DateTime(this HtmlHelper htmlHelper, DateTime value)
        {
            TimeSpan time = System.DateTime.UtcNow - value;

            if (time.TotalDays > 7)
                //TODO: (erikpo) This format should come from a site setting
                return "at " + value.ToString("MMM d yyyy h:mm tt");
            if (time.TotalHours > 24)
                return string.Format("{0} day{1} ago", time.Days, time.Days == 1 ? "" : "s");
            if (time.TotalMinutes > 60)
                return string.Format("{0} hour{1} ago", time.Hours, time.Hours == 1 ? "" : "s");
            if (time.TotalSeconds > 60)
                return string.Format("{0} minute{1} ago", time.Minutes, time.Minutes == 1 ? "" : "s");
            else if (time.TotalSeconds > 10)
                return string.Format("{0} second{1} ago", time.Seconds, time.Seconds == 1 ? "" : "s");
            else
                return "a moment ago";
        }

        public static string DateTime(this HtmlHelper htmlHelper, DateTime? value, string defaultIfNull) {
            return value.HasValue ? htmlHelper.DateTime(value.Value) : defaultIfNull;
        }

        #endregion

        #region Link

        public static string Link(this HtmlHelper htmlHelper, string linkContents, string href)
        {
            return htmlHelper.Link(linkContents, href, null);
        }

        public static string Link(this HtmlHelper htmlHelper, string linkContents, string href, object htmlAttributes)
        {
            return htmlHelper.Link(linkContents, href, new RouteValueDictionary(htmlAttributes));
        }

        public static string Link(this HtmlHelper htmlHelper, string linkContents, string href, IDictionary<string, object> htmlAttributes)
        {
            TagBuilder tagBuilder = new TagBuilder("a")
            {
                InnerHtml = linkContents
            };
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("href", href);
            return tagBuilder.ToString(TagRenderMode.Normal);
        }

        #endregion

        #region LinkOrDefault

        public static string LinkOrDefault(this HtmlHelper htmlHelper, string linkContents, string href)
        {
            return htmlHelper.LinkOrDefault(linkContents, href, null);
        }

        public static string LinkOrDefault(this HtmlHelper htmlHelper, string linkContents, string href, object htmlAttributes)
        {
            return htmlHelper.LinkOrDefault(linkContents, href, new RouteValueDictionary(htmlAttributes));
        }

        public static string LinkOrDefault(this HtmlHelper htmlHelper, string linkContents, string href, IDictionary<string, object> htmlAttributes)
        {
            if (!string.IsNullOrEmpty(href))
            {
                TagBuilder tagBuilder = new TagBuilder("a")
                {
                    InnerHtml = linkContents
                };
                tagBuilder.MergeAttributes(htmlAttributes);
                tagBuilder.MergeAttribute("href", href);
                linkContents = tagBuilder.ToString(TagRenderMode.Normal);
            }

            return linkContents;
        }

        #endregion
    }
}
