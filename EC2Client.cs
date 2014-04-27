using SimpleAWS.Models;
using SimpleAWS.Models.EC2;
using SimpleAWS.Models.S3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SimpleAWS
{
    public class EC2Client
    {
        private string URL { get; set; }
        private string AccessKey { get; set; }
        private string SecretKey { get; set; }
        private int RetryCount { get; set; }

        public EC2Client(string accessKey, string secretKey, int retryCount = 0, string url = "https://ec2.amazonaws.com/")
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            URL = url;
            AccessKey = accessKey;
            SecretKey = secretKey;
            RetryCount = retryCount;
        }

        public DescribeInstancesResponse DescribeInstances(DescribeInstancesRequest request)
        {
            return Util.RetryMethod<DescribeInstancesResponse>(() => DoDescribeInstances(request), RetryCount);
        }

        public TerminateInstancesResponse TerminateInstances(TerminateInstancesRequest request)
        {
            return Util.RetryMethod<TerminateInstancesResponse>(() => DoTerminateInstances(request), RetryCount);
        }

        public RunInstancesResponse RunInstances(RunInstancesRequest request)
        {
            return Util.RetryMethod<RunInstancesResponse>(() => DoRunInstances(request), RetryCount);
        }

        public DescribeInstancesResponse DoDescribeInstances(DescribeInstancesRequest request)
        {
            List<string> lParams = new List<string>();

            for (int i = 0; i < request.Filters.Count; i++)
            {
                var filter = request.Filters[i];

                lParams.Add(string.Format("&Filter.{0}.Name={1}", i + 1, Util.UrlEncode(filter.Name)));

                for (int a = 0; a < filter.Values.Count; a++)
                {
                    lParams.Add(string.Format("&Filter.{0}.Value.{1}={2}", i + 1, a + 1, Util.UrlEncode(filter.Values[a])));
                }
            }

            lParams.Sort();

            string parameters = string.Format("AWSAccessKeyId={0}&Action={1}{2}&SignatureMethod={3}&SignatureVersion={4}&Timestamp={5}&Version={6}",
                Util.UrlEncode(AccessKey),
                "DescribeInstances",
                string.Join("", lParams),
                "HmacSHA256",
                "2",
                Util.UrlEncode(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                "2012-07-20");

            string sig = Util.GetSignature(URL, "GET", parameters, SecretKey);
            parameters = string.Format("{0}&Signature={1}", parameters, Util.UrlEncode(sig));

            var wRequest = WebRequest.Create(string.Format("{0}?{1}", URL, parameters)) as HttpWebRequest;
            wRequest.Method = "GET";
            wRequest.ContentType = "application/x-www-form-urlencoded";
            wRequest.KeepAlive = false;

            using (var response = wRequest.GetResponse() as HttpWebResponse)
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                var body = reader.ReadToEnd();
                var element = XElement.Parse(body);

                XmlSerializer serializer = new XmlSerializer(typeof(DescribeInstancesResponse));
                var describeInstancesResponse = (DescribeInstancesResponse)serializer.Deserialize(element.CreateReader());
                return describeInstancesResponse;
            }
        }

        public TerminateInstancesResponse DoTerminateInstances(TerminateInstancesRequest request)
        {
            List<string> lParams = new List<string>();

            for (int i = 0; i < request.InstanceIds.Count; i++)
            {
                lParams.Add(string.Format("&InstanceId.{0}={1}", i + 1, Util.UrlEncode(request.InstanceIds[i])));
            }

            lParams.Sort();

            string parameters = string.Format("AWSAccessKeyId={0}&Action={1}{2}&SignatureMethod={3}&SignatureVersion={4}&Timestamp={5}&Version={6}",
                Util.UrlEncode(AccessKey),
                "TerminateInstances",
                string.Join("", lParams),
                "HmacSHA256",
                "2",
                Util.UrlEncode(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                "2012-07-20");

            string sig = Util.GetSignature(URL, "GET", parameters, SecretKey);
            parameters = string.Format("{0}&Signature={1}", parameters, Util.UrlEncode(sig));

            var wRequest = WebRequest.Create(string.Format("{0}?{1}", URL, parameters)) as HttpWebRequest;
            wRequest.Method = "GET";
            wRequest.ContentType = "application/x-www-form-urlencoded";
            wRequest.KeepAlive = false;

            using (var response = wRequest.GetResponse() as HttpWebResponse)
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                var body = reader.ReadToEnd();
                var element = XElement.Parse(body);

                XmlSerializer serializer = new XmlSerializer(typeof(TerminateInstancesResponse));
                var terminateInstancesResponse = (TerminateInstancesResponse)serializer.Deserialize(element.CreateReader());
                return terminateInstancesResponse;
            }
        }

        public RunInstancesResponse DoRunInstances(RunInstancesRequest request)
        {
            List<string> lParams = new List<string>();

            var propValues = typeof(RunInstancesRequest).GetProperties();

            for (int i = 0; i < propValues.Length; i++)
            {
                if (propValues[i].PropertyType.IsValueType || propValues[i].PropertyType == typeof(string))
                {
                    var value = propValues[i].GetValue(request);

                    if (propValues[i].PropertyType == typeof(bool))
                        value = value.ToString().ToLower();

                    if (value != null)
                    {
                        FieldInfo info = propValues[i].PropertyType.GetField(value.ToString());

                        if (info != null && info.IsDefined(typeof(XmlEnumAttribute), false))
                        {
                            object[] o = info.GetCustomAttributes(typeof(XmlEnumAttribute), false);
                            XmlEnumAttribute att = (XmlEnumAttribute)o[0];
                            value = att.Name;
                        }

                        lParams.Add(string.Format("&{0}={1}", propValues[i].Name, Util.UrlEncode(value.ToString())));
                    }
                }
                else if (propValues[i].PropertyType == typeof(List<string>))
                {
                    var value = (List<string>)propValues[i].GetValue(request);

                    for (int a = 0; a < value.Count; a++)
                    {
                        lParams.Add(string.Format("&{0}.{1}={2}", propValues[i].Name, a + 1, Util.UrlEncode(value[a])));
                    }
                }
            }

            lParams.Add(string.Format("&SignatureMethod={0}", "HmacSHA256"));
            lParams.Add(string.Format("&SignatureVersion={0}", "2"));
            lParams.Add(string.Format("&Timestamp={0}", Util.UrlEncode(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))));
            lParams.Add(string.Format("&Version={0}", "2012-07-20"));

            lParams.Sort();

            string parameters = string.Format("AWSAccessKeyId={0}&Action={1}{2}",
                Util.UrlEncode(AccessKey),
                "RunInstances",
                string.Join("", lParams));

            string sig = Util.GetSignature(URL, "GET", parameters, SecretKey);
            parameters = string.Format("{0}&Signature={1}", parameters, Util.UrlEncode(sig));

            var wRequest = WebRequest.Create(string.Format("{0}?{1}", URL, parameters)) as HttpWebRequest;
            wRequest.Method = "GET";
            wRequest.ContentType = "application/x-www-form-urlencoded";
            wRequest.KeepAlive = false;

            using (var response = wRequest.GetResponse() as HttpWebResponse)
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                var body = reader.ReadToEnd();
                var element = XElement.Parse(body);

                XmlSerializer serializer = new XmlSerializer(typeof(RunInstancesResponse));
                var runInstancesResponse = (RunInstancesResponse)serializer.Deserialize(element.CreateReader());
                return runInstancesResponse;
            }
        }
    }
}
