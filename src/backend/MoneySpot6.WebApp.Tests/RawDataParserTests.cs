using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Features.AccountSync.Services;
using Shouldly;

namespace MoneySpot6.WebApp.Tests
{
    internal class RawDataParserTests
    {
        private static DbBankAccountTransactionParsedData Parse(DbBankAccountTransactionRawData raw) => new RawDataParser(new SepaParser2()).Parse(raw);

        [Test]
        public void Test1()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                          KREF+2024-07-03T00:59:10:01
                          12 
                          SVWZ+Schulden TAN1:123456 I
                          BAN: DE32845686328746832746
                           BIC: DEXXGHFXS 
                          """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                CustomerReference = "2024-07-03T00:59:10:0112",
                Purpose = "Schulden",
                IBAN = "DE32845686328746832746",
                BIC = "DEXXGHFXS",
            });
        }

        [Test]
        public void Test2()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                          SVWZ+Ein komischer.Verwendungszweck 
                          auf mehreren Zeilen 
                          Hier kommt nochwas
                          ABWA+Das ist ein..Test//Text 
                          mit Zeilen-Umbruch
                          """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = "Ein komischer.Verwendungszweck auf mehreren Zeilen Hier kommt nochwas",
                AlternateInitiator = "Das ist ein..Test//Text mit Zeilen-Umbruch"
            });
        }

        [Test]
        public void Test3()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                  SVWZ+Das folgende Tag gibts nicht
                  Fooo
                  ABCD+Gehoert zum Verwendungszweck
                  """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = "Das folgende Tag gibts nichtFoooABCD+Gehoert zum Verwendungszweck",
            });
        }

        [Test]
        public void Test4()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                  SVWZ+Das folgende Tag gibts nicht
                  Fooo 
                  ABCD+ Leerzeichen hinter dem Tag stoeren nicht
                  KREF+ und hier stoeren sie auch nicht, sind aber nicht teil des Value
                  """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = "Das folgende Tag gibts nichtFooo ABCD+ Leerzeichen hinter dem Tag stoeren nicht",
                CustomerReference = "und hier stoeren sie auch nicht, sind aber nicht teil des Value"
            });
        }

        [Test]
        public void Test5()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                  SVWZ+Nur eine Zeile
                  """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = "Nur eine Zeile"
            });
        }

        [Test]
        public void Test6()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                  SVWZ+
                  """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = ""
            });
        }

        [Test]
        public void Test7()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = ""
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = ""
            });
        }

        [Test]
        public void Test8()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                  Das ist eine Zeile ohne Tag 
                  Fooo
                  KREF+Und hier kommen ploetzlich noch Tags. Der Teil bis zum ersten Tag ist dann eigentlich der Verwendungszweck
                  """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = "Das ist eine Zeile ohne Tag Fooo",
                CustomerReference = "Und hier kommen ploetzlich noch Tags. Der Teil bis zum ersten Tag ist dann eigentlich der Verwendungszweck"
            });
        }

        [Test]
        public void Test9()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                  Wir koennen auch
                  mit
                  KREF: Doppelpunkt als Separatur umgehen
                  """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = "Wir koennen auchmit",
                CustomerReference = "Doppelpunkt als Separatur umgehen"
            });
        }

        [Test]
        public void Test10()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                  SVWZ+ Das geht sogar
                   gemischt 
                  IBAN: DE1234567890 
                  """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = "Das geht sogar gemischt",
                IBAN = "DE1234567890"
            });
        }

        [Test]
        public void Test11()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                  IBAN: DE49390500000000012345 BIC: AACSDE33 ABWA: NetAachen
                  """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = "",
                IBAN = "DE49390500000000012345",
                BIC = "AACSDE33",
                AlternateInitiator = "NetAachen"
            });
        }

        [Test]
        public void Test12()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                  Verwendungszweck EREF: 1234
                  567890123456789 IBAN: DE123
                  45678901234567890 BIC: ABCD
                  EFGH
                  """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = "Verwendungszweck",
                EndToEndReference = "1234567890123456789",
                IBAN = "DE12345678901234567890",
                BIC = "ABCDEFGH"
            });
        }

        [Test]
        public void Test13()
        {
            var raw = new DbBankAccountTransactionRawData
            {
                Date = new DateOnly(2024, 1, 1),
                Counterparty = new CounterpartyAccount(),
                Purpose = """
                  SVWZ+Das ist Zeile 1
                  2
                  3
                  """
            };

            var result = Parse(raw);

            result.ShouldBeEquivalentTo(new DbBankAccountTransactionParsedData
            {
                Purpose = "Das ist Zeile 123"
            });
        }

    }
}
