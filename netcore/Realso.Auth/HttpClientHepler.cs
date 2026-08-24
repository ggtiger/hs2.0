using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace Realso.Auth
{
  public class HttpClientHepler
  {
    string _url;
    public string Url
    {
      get
      {
        return _url;
      }

      set
      {
        _url = value;
      }
    }

    public HttpClientHepler(string url)
    {
      Url = url;
    }

    public async Task GetAsync(string queryString, Action<HttpRequestHeaders> addHeader,
        Action<string> okAction = null,
        Action<HttpResponseMessage> faultAction = null, Action<Exception> exAction = null)
    {
      using (HttpClient client = new HttpClient())
      {
        addHeader(client.DefaultRequestHeaders);
        using (HttpResponseMessage response = await client.GetAsync(Url + "?" + queryString))
        {
          try
          {
            if (response.IsSuccessStatusCode)
            {
              okAction(await response.Content.ReadAsStringAsync());
            }
            else
            {
              faultAction?.Invoke(response);
            }
          }
          catch (Exception ex)
          {
            exAction?.Invoke(ex);
          }
        }
      }
    }

    public async Task PostAsync(string queryString, string content, Action<HttpContentHeaders> addHeader,
        Action<string> okAction = null,
        Action<HttpResponseMessage> faultAction = null, Action<Exception> exAction = null)
    {
      using (HttpClient client = new HttpClient())
      {
        using (HttpContent httpContent = new StringContent(content))
        {
          addHeader(httpContent.Headers);
          using (HttpResponseMessage response = await client.PostAsync(Url + "?" + queryString, httpContent))
          {
            try
            {
              if (response.IsSuccessStatusCode)
              {
                okAction(await response.Content.ReadAsStringAsync());
              }
              else
              {
                faultAction?.Invoke(response);
              }
            }
            catch (Exception ex)
            {
              exAction?.Invoke(ex);
            }
          }
        }
      }
    }


  }
}
