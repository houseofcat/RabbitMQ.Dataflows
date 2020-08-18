using HouseofCat.Utilities.Random;
using System;
using System.Collections.Generic;

namespace HouseofCat.RabbitMQ
{
    /// <summary>
    /// Static class for generating filler (random) data for users and Tests.
    /// </summary>
    public static class RandomData
    {
        private static readonly XorShift XorShift = new XorShift(true);

        public static Letter CreateSimpleRandomLetter(string queueName, int bodySize = 1000)
        {
            var payload = new byte[bodySize];
            XorShift.FillBuffer(payload, 0, bodySize);

            return new Letter
            {
                LetterId = 0,
                LetterMetadata = new LetterMetadata(),
                Envelope = new Envelope
                {
                    Exchange = string.Empty,
                    RoutingKey = queueName,
                    RoutingOptions = new RoutingOptions
                    {
                        DeliveryMode = 1,
                        PriorityLevel = 0
                    }
                },
                Body = payload
            };
        }

        public static IList<Letter> CreateManySimpleRandomLetters(List<string> queueNames, int letterCount, int bodySize = 1000)
        {
            var random = new Random();
            var letters = new List<Letter>();

            var queueCount = queueNames.Count;
            for (int i = 0; i < letterCount; i++)
            {
                letters.Add(CreateSimpleRandomLetter(queueNames[random.Next(0, queueCount)], bodySize));
            }

            return letters;
        }

        public static IList<Letter> CreateManySimpleRandomLetters(string queueName, int letterCount, int bodySize = 1000)
        {
            var letters = new List<Letter>();

            for (int i = 0; i < letterCount; i++)
            {
                letters.Add(CreateSimpleRandomLetter(queueName, bodySize));
            }

            return letters;
        }
    }
}
