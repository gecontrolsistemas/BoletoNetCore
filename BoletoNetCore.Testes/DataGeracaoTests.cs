using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace BoletoNetCore.Testes
{
    /// <summary>
    /// Data e hora de geração do arquivo de remessa: quem gera pode informá-las, em vez de a
    /// biblioteca ler o relógio do processo.
    /// </summary>
    /// <remarks>
    /// Um processo em UTC — um contêiner, por exemplo — carimba o dia seguinte em todo arquivo
    /// gerado depois das 21h no horário de Brasília. O campo fica divergindo do sequencial e do
    /// que o beneficiário registrou, e não havia como corrigir de fora sem reescrever o arquivo
    /// pronto por posição.
    /// </remarks>
    [TestFixture]
    [Category("Data de geração")]
    public class DataGeracaoTests
    {
        private static readonly DateTime Gravacao = new DateTime(2026, 3, 17, 22, 45, 31);

        private IBanco _banco;

        [SetUp]
        public void Preparar()
        {
            var contaBancaria = new ContaBancaria
            {
                Agencia = "1234",
                DigitoAgencia = "X",
                Conta = "123456",
                DigitoConta = "X",
                CarteiraPadrao = "09",
                TipoCarteiraPadrao = TipoCarteira.CarteiraCobrancaSimples,
                TipoFormaCadastramento = TipoFormaCadastramento.ComRegistro,
                TipoImpressaoBoleto = TipoImpressaoBoleto.Empresa
            };

            _banco = Banco.Instancia(Bancos.Bradesco);
            _banco.Beneficiario = TestUtils.GerarBeneficiario("1213141", "", "", contaBancaria);
            _banco.FormataBeneficiario();
        }

        [TearDown]
        public void Encerrar()
        {
            // A instância vem de Banco.Instancia e é a mesma no processo inteiro: sem devolver o
            // relógio da máquina, a data fixada aqui valeria para todo teste que rodasse depois.
            _banco.DataGeracao = default(DateTime);
        }

        [Test]
        public void Sem_atribuicao_a_data_de_geracao_e_o_relogio_da_maquina()
        {
            var antes = DateTime.Now;

            var lida = _banco.DataGeracao;

            Assert.That(lida, Is.InRange(antes, DateTime.Now));
        }

        [Test]
        public void Data_atribuida_e_a_que_vai_para_o_header_da_remessa()
        {
            _banco.DataGeracao = Gravacao;

            // CNAB400: a data de gravação do arquivo ocupa as posições 095-100, no formato DDMMAA.
            var header = GerarRemessa(TipoArquivo.CNAB400).Split('\n')[0];

            Assert.That(header.Substring(94, 6), Is.EqualTo("170326"));
        }

        [Test]
        public void Data_atribuida_vale_tambem_para_o_header_do_lote_no_CNAB240()
        {
            _banco.DataGeracao = Gravacao;

            var linhas = GerarRemessa(TipoArquivo.CNAB240).Split('\n');

            // Header do arquivo: data em 144-151 (DDMMAAAA) e hora em 152-157 (HHMMSS).
            Assert.That(linhas[0].Substring(143, 8), Is.EqualTo("17032026"));
            Assert.That(linhas[0].Substring(151, 6), Is.EqualTo("224531"));

            // Header do lote: a mesma data em 192-199. Um arquivo com duas datas diferentes é o
            // que acontecia quando só o primeiro carimbo era corrigido de fora.
            Assert.That(linhas[1].Substring(191, 8), Is.EqualTo("17032026"));
        }

        [Test]
        public void Data_atribuida_vale_para_o_nome_do_arquivo()
        {
            _banco.DataGeracao = Gravacao;

            var arquivo = new ArquivoRemessa(_banco, TipoArquivo.CNAB400, 1);

            Assert.That(arquivo.NomeArquivo, Does.StartWith("CB1703"));
        }

        [Test]
        public void Com_a_mesma_data_o_arquivo_sai_identico()
        {
            // Os mesmos boletos nas duas gerações: TestUtils.GerarBoletos numera a partir de um
            // contador estático, e dois lotes gerados na sequência já diferem pelo nosso número.
            var boletos = TestUtils.GerarBoletos(_banco, 1, "N", 223344);

            _banco.DataGeracao = Gravacao;
            var primeiro = GerarRemessa(TipoArquivo.CNAB400, boletos);

            _banco.DataGeracao = Gravacao;
            var segundo = GerarRemessa(TipoArquivo.CNAB400, boletos);

            Assert.That(segundo, Is.EqualTo(primeiro));
        }

        [Test]
        public void Atribuir_o_valor_padrao_devolve_o_relogio_da_maquina()
        {
            _banco.DataGeracao = Gravacao;
            _banco.DataGeracao = default(DateTime);

            Assert.That(_banco.DataGeracao.Date, Is.EqualTo(DateTime.Now.Date));
        }

        private string GerarRemessa(TipoArquivo tipoArquivo, Boletos boletos = null)
        {
            boletos = boletos ?? TestUtils.GerarBoletos(_banco, 1, "N", 223344);
            var arquivo = new ArquivoRemessa(_banco, tipoArquivo, 1);

            using (var stream = new MemoryStream())
            {
                arquivo.GerarArquivoRemessa(boletos, stream);
                return Encoding.GetEncoding("ISO-8859-1").GetString(stream.ToArray());
            }
        }
    }
}
