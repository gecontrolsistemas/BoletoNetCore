# Gecontrol.BoletoNetCore

Fork de [BoletoNetCore](https://github.com/BoletoNet/BoletoNetCore) (MIT), mantido pela Gecontrol
Sistemas. **Não é o pacote `BoletoNetCore` do nuget.org** — o id é outro de propósito, para que
ninguém restaure um achando que restaurou o outro.

## Como o fork é organizado

| Branch | O que é |
|---|---|
| `master` | espelho do upstream, sem commit próprio |
| `gecontrol` | os patches nossos sobre o espelho, um commit por assunto, com teste |

Cada patch nasce **candidato a PR para o upstream**: patch aceito lá em cima é patch que a gente
para de manter. Este pacote sai do `gecontrol`.

## Versão

`3.0.1.563-gecontrol.N`

- `3.0.1.563` — o `master` do upstream um commit além do `3.0.1.562` publicado no nuget.org em
  24/08/2026;
- `gecontrol.N` — o número do nosso conjunto de patches sobre essa base.

A versão é **imutável**: um número publicado nunca é reaproveitado. Quem consome grava a versão
que gerou cada arquivo, e reaproveitar tornaria esse registro mentira. O commit exato está no
campo `repository` do nuspec.

## Patches nesta versão

### `IBanco.DataGeracao` — a data de gravação da remessa vem de quem gera

Os campos de data e hora de gravação do arquivo de remessa, e o nome do arquivo, saíam de
`DateTime.Now`. Num processo que roda em UTC — um contêiner, por exemplo —, todo arquivo gerado
depois das 21h no horário de Brasília sai datado do dia seguinte, divergindo do sequencial do dia.

```csharp
var banco = Banco.Instancia(237);
banco.DataGeracao = new DateTime(2026, 3, 17, 22, 45, 31); // relógio civil de quem emite

var arquivo = new ArquivoRemessa(banco, TipoArquivo.CNAB400, sequencial);
arquivo.GerarArquivoRemessa(boletos, stream);
```

Sem atribuição, cada leitura devolve `DateTime.Now` — o comportamento que a biblioteca sempre
teve, byte a byte. Atribuir `default(DateTime)` volta a ele, o que importa porque
`Banco.Instancia` devolve a **mesma instância** no processo inteiro.

O efeito colateral útil é o arquivo determinístico: com a data informada, duas gerações do mesmo
lote saem idênticas, e a remessa passa a ser testável byte a byte.
