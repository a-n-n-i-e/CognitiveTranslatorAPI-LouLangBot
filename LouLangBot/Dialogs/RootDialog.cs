using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;
using System.Net.Http;
using TranslatorService;

namespace LouLangBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        // API Key, Region のセット
        static string textAnalyticsSubKey = "YOUR_TEXTANALYTICS_API_KEY";
        static AzureRegions textAnalyticsRegion = AzureRegions.Westus;

        static string translatorSubKey = "YOUR_TRANSLATOR_API_KEY";


        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            var userText = activity.Text;

            if (userText == null)
            {
                await context.PostAsync($"こんにちは！ルー語BOTだよ。ルー語にしたい文章を入れてみてね。");
            }
            else
            {
                // API Client の作成
                var textAnalyticsClient = new TextAnalyticsAPI();
                textAnalyticsClient.AzureRegion = textAnalyticsRegion;
                textAnalyticsClient.SubscriptionKey = textAnalyticsSubKey;
                var translatorClient = new TranslatorServiceClient(translatorSubKey);

                var keyPhraseJa = new List<string>();
                var keyPhraseEn = new List<string>();

                // Text Analytics API を利用したキーワードの取得
                try
                {
                    var textAnalyticsResult = textAnalyticsClient.KeyPhrases(
                        new MultiLanguageBatchInput(
                            new List<MultiLanguageInput>()
                            {
                                new MultiLanguageInput("ja","1",userText)
                            }));

                    foreach (var keyPhrase in textAnalyticsResult.Documents[0].KeyPhrases)
                    {
                        //await context.PostAsync($"Keyword= " + keyPhrase);
                        keyPhraseJa.Add(keyPhrase);
                    }
                }
                catch
                {
                    await context.PostAsync($"Error: TextAnalytics API");
                }

                // Translator API を利用したキーワードの翻訳(日本語→英語)
                try
                {
                    foreach (var keyPhrase in keyPhraseJa)
                    {
                        var res = await translatorClient.TranslateAsync(keyPhrase, to: "en-us");
                        //await context.PostAsync($"Keyword in English = " + res);
                        keyPhraseEn.Add(res);

                    }
                }
                catch
                {
                    await context.PostAsync($"Error: Translator API");

                }

                // キーワード(日本語→英語)の入れ替えと返答
                for (int i = 0; i < keyPhraseJa.Count; i++)
                {
                    userText = userText.Replace(keyPhraseJa[i], keyPhraseEn[i]);

                }

                await context.PostAsync(userText);

                context.Wait(MessageReceivedAsync);
            }
        }
    }
}