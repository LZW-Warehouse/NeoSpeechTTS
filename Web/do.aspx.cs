using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;


namespace Web
{
	public partial class _do : System.Web.UI.Page
	{
		public bool Success = false;
		public string ErrorReason = String.Empty;
		public MemoryStream Stream = new MemoryStream();
		SpeechSynthesizer SpeechSynthesizer = new SpeechSynthesizer();

		/*GET参数列表
			 * msg：内容。不可为空sfsfdsdfsdfggdfgdfgdf
			 * voiceName：语音名称。通过名称选择特定语音。默认为VW Liang
			 * type：类型。1播放，2下载。默认为1
			 */
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				string msg = Request.QueryString["msg"];
				string voiceName = Request.QueryString["voiceName"];
				string type = Request.QueryString["type"];
				#region 验证
				//msg
				if (String.IsNullOrWhiteSpace(msg))
				{
					Close();
					Response.Write("请输入内容！");
					return;
				}
				else
				{
					msg = msg.Trim();
				}
				msg = HttpUtility.UrlDecode(msg);
				//voiceName
				if (String.IsNullOrWhiteSpace(voiceName))
				{
					voiceName = System.Configuration.ConfigurationManager.AppSettings["SelectVoice"];
					if (String.IsNullOrWhiteSpace(voiceName))
					{
						Close();
						Response.Write("没有配置默认语音，请输入语音名称！");
						return;
					}
					voiceName = voiceName.Trim();
				}
				else
				{
					voiceName = voiceName.Trim();
				}
				//type
				if (String.IsNullOrWhiteSpace(type))
				{
					type = "1";
				}
				else
				{
					type = type.Trim();
				}
				if (type != "1" && type != "2")
				{
					Close();
					Response.Write("类型不存在！");
					return;
				}
				#endregion

				Task.Factory.StartNew(() => Exec(msg, voiceName, type));

				int i = 0;
				while (true)
				{
					if (Success)
					{
						Response.Clear();
						Response.BufferOutput = true;
						if (type == "2")
						{
							Response.ContentType = "audio/mpeg";
							Response.AddHeader("Content-Length", Stream.Length.ToString());
							Response.AddHeader("Content-Disposition", "attachment; filename=\"" + HttpUtility.UrlEncode(System.Guid.NewGuid().ToString(), System.Text.Encoding.UTF8) + ".wav\";");
							Stream.WriteTo(Response.OutputStream);
						}
						Response.Flush();
						Close();
						break;
					}
					if (!String.IsNullOrWhiteSpace(ErrorReason))
					{
						Close();
						Response.Write(ErrorReason);
						break;
					}
					if (i > 15)
					{
						Close();
						Response.Write("超时！");
						break;
					}
					i++;
					Thread.Sleep(1000);
				}
			}
			Response.End();
		}

		public void Exec(string msg, string voiceName, string type)
		{
			try
			{
				var voiceList = SpeechSynthesizer.GetInstalledVoices();
				if (voiceList == null || voiceList.Count <= 0)
				{
					Close();
					ErrorReason = "没有可用的语音！";
					return;
				}
				bool isExiste = false;
				string voiceNameList = "";
				foreach (var item in voiceList)
				{
					if (item.Enabled == true && item.VoiceInfo != null)
					{
						voiceNameList += item.VoiceInfo.Name;
						if (item.VoiceInfo.Name == voiceName)
						{
							isExiste = true;
						}
					}
				}
				if (!isExiste)
				{
					Close();
					ErrorReason = "语音名称不存在！请选择下列语音：" + voiceNameList;
					return;
				}

				SpeechSynthesizer.SelectVoice(voiceName);
				SpeechSynthesizer.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(Exec_SpeakCompleted);
				if (type == "1")
				{
					SpeechSynthesizer.SetOutputToDefaultAudioDevice();
				}
				else
				{
					SpeechSynthesizer.SetOutputToWaveStream(Stream);
				}
				SpeechSynthesizer.SpeakAsync(msg);
			}
			catch (Exception ex)
			{
				ErrorReason = "异常! 详细：" + ex.ToString();
			}
		}

		public void Exec_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
		{
			Success = true;
		}

		public void Close()
		{
			if (Stream != null)
			{
				Stream.Dispose();
				Stream.Close();
			}
			if (SpeechSynthesizer != null)
			{
				SpeechSynthesizer.Dispose();
			}
		}
	}
}
