using System;
using NUnit.Framework;

namespace BoletoNetCore.Testes
{
    /// <summary>
    /// Leitura do retorno de Varredura DDA (Débito Direto Autorizado) - segmento G do CNAB240.
    /// Diferente do retorno de cobrança (T/U), a varredura traz os boletos registrados a pagar
    /// contra o sacado. Validado com um registro de segmento G montado nas posições do layout.
    /// </summary>
    [TestFixture]
    [Category("Itau Varredura DDA")]
    public class BancoItauRetornoVarreduraDdaTests
    {
        // Monta uma linha CNAB240 (240 posições preenchidas com espaço) e aplica os campos por posição.
        private static string MontarRegistro(Action<char[]> preencher)
        {
            var registro = new string(' ', 240).ToCharArray();
            preencher(registro);
            return new string(registro);
        }

        private static void Set(char[] registro, int posicao, string valor)
        {
            for (int i = 0; i < valor.Length; i++)
                registro[posicao + i] = valor[i];
        }

        [Test]
        public void Deve_ler_segmento_G_da_varredura_dda_itau()
        {
            var codigoBarras = "3419".PadRight(44, '0'); // 44 posições (conteúdo sintético)

            var segmentoG = MontarRegistro(r =>
            {
                Set(r, 0, "341");               // código do banco
                Set(r, 7, "3");                 // tipo de registro (detalhe)
                Set(r, 13, "G");                // segmento
                Set(r, 15, "01");               // código de movimento (entrada de títulos)
                Set(r, 17, codigoBarras);       // campo livre / código de barras (44)
                Set(r, 107, "17062026");        // vencimento (DDMMAAAA)
                Set(r, 115, "000000001806455"); // valor (15) => 18.064,55
                Set(r, 147, "987654321098765"); // número do documento (15) - posições 148-162
                Set(r, 61, "2");                 // tipo de inscrição do cedente ('2' = CNPJ)
                Set(r, 62, "043187431000111");   // nº inscrição do cedente 9(15) => CNPJ 43187431000111
                Set(r, 77, "BRASLIGHT INDUSTRIA COMERCIO E"); // nome do cedente X(30)
                Set(r, 181, "01062026");        // data de emissão do título (DDMMAAAA) - 182-189
                Set(r, 231, "20082026");        // data limite para pagamento (DDMMAAAA) - 232-239
            });

            var banco = (IBancoCNAB240)Banco.Instancia(341);
            var boleto = new Boleto((IBanco)banco, ignorarCarteira: true);

            banco.LerDetalheRetornoCNAB240SegmentoG(ref boleto, segmentoG);

            Assert.AreEqual(new DateTime(2026, 6, 17), boleto.DataVencimento, "Vencimento");
            Assert.AreEqual(18064.55M, boleto.ValorTitulo, "Valor do título");
            Assert.AreEqual("987654321098765", boleto.NumeroDocumento, "Número do documento");
            Assert.AreEqual(codigoBarras, boleto.CodigoBarra.CodigoDeBarras, "Código de barras");
            Assert.AreEqual("01", boleto.CodigoMovimentoRetorno, "Código de movimento");
            Assert.AreEqual("Entrada de Títulos", boleto.DescricaoMovimentoRetorno, "Descrição do movimento (varredura DDA)");
            Assert.IsNotEmpty(boleto.CodigoBarra.LinhaDigitavel, "Linha digitável derivada do código de barras");
            Assert.AreEqual("43187431000111", boleto.Cedente.CPFCNPJ, "CNPJ do cedente (fornecedor)");
            Assert.AreEqual("BRASLIGHT INDUSTRIA COMERCIO E", boleto.Cedente.Nome, "Nome do cedente");
            Assert.AreEqual(new DateTime(2026, 6, 1), boleto.DataEmissao, "Data de emissão do título");
            Assert.AreEqual(new DateTime(2026, 8, 20), boleto.DataLimitePagamento, "Data limite para pagamento");
        }
    }
}
