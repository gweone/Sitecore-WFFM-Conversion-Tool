﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WFFM.ConversionTool.Library.Helpers;
using WFFM.ConversionTool.Library.Logging;
using WFFM.ConversionTool.Library.Models.Form;
using WFFM.ConversionTool.Library.Repositories;

namespace WFFM.ConversionTool.Library.Converters.SubmitActionConverters
{
	public class SendEmailConverter : BaseFieldConverter
	{
		private IDestMasterRepository _destMasterRepository;
		private ILogger _logger;

		public SendEmailConverter(IDestMasterRepository destMasterRepository, ILogger logger)
		{
			_destMasterRepository = destMasterRepository;
			_logger = logger;
		}

		public override string ConvertValue(string sourceValue)
		{
			// example of sourceValue
			// <host>example.host</host><from>example@mail.net</from><isbodyhtml>true</isbodyhtml><to>to@example.com</to><cc>cc@example.com</cc><bcc>bcc@example.com</bcc><localfrom>example@mail.net</localfrom><subject>This is the subject of the email.</subject><mail><p>This is the body of the email.</p><p>[<label id="{CFA55E36-3018-41A4-9F4D-2EA1293D5902}">Single-Line Text</label>]</p></mail>
			var host = XmlHelper.GetXmlElementValue(sourceValue, "host");
			var from = XmlHelper.GetXmlElementValue(sourceValue, "from");
			var isbodyhtml = XmlHelper.GetXmlElementValue(sourceValue, "isbodyhtml");
			var to = XmlHelper.GetXmlElementValue(sourceValue, "to");
			var cc = XmlHelper.GetXmlElementValue(sourceValue, "cc");
			var bcc = XmlHelper.GetXmlElementValue(sourceValue, "bcc");
			var localfrom = XmlHelper.GetXmlElementValue(sourceValue, "localfrom");
			var subject = ConvertFieldTokens(XmlHelper.GetXmlElementValue(sourceValue, "subject"));
			var mail = ConvertFieldTokens(XmlHelper.GetXmlElementValue(sourceValue, "mail"));

			var fromValue = !string.IsNullOrEmpty(from) ? from : localfrom;

			return JsonConvert.SerializeObject(new
				SendEmailAction()
			{
				from = fromValue,
				to = to,
				cc = cc,
				bcc = bcc,
				subject = subject,
				body = mail
			});
		}

		private string ConvertFieldTokens(string fieldText)
		{
			// Find all tokens
			var matches = Regex.Matches(fieldText, @"\[(.*?)\]", RegexOptions.IgnoreCase);

			foreach (Match match in matches)
			{
				Guid fieldId;
				var fieldName = string.Empty;
				var matchValue = match.Value.Replace("[", "").Replace("]", "");
				if (Guid.TryParse(matchValue, out fieldId)) // case of token in subject field
				{
					// find field name
					fieldName = _destMasterRepository.GetSitecoreItem(fieldId)?.Name;

				}
				else // case of token in message field
				{
					// get field label value
					fieldName = XmlHelper.GetXmlElementValue(matchValue, "label");
				}

				if (!string.IsNullOrEmpty(fieldName))
				{
					// replace token with label value
					fieldText = fieldText.Replace(matchValue, fieldName);
				}
			}

			return fieldText;
		}
	}
}
