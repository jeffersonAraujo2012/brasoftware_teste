# Diretrizes do repositório

## Estrutura do projeto

O repositório começou com um `README.md` vazio na raiz. Ainda não há diretórios de código-fonte, testes ou recursos. Ao adicionar a primeira implementação, siga a estrutura recomendada pelo framework adotado e documente no `README.md` o ponto de entrada e a função de cada diretório. Mantenha as orientações para contribuições neste `AGENTS.md`. Escreva os documentos e arquivos Markdown em português brasileiro.

## Compilação, testes e desenvolvimento

Ainda não há comandos configurados para compilar, executar, testar ou rodar o linter. Ao adicionar ferramentas, registre no `README.md` os pré-requisitos e os comandos para instalar dependências, executar o projeto localmente, compilar e rodar os testes. Por enquanto, use os comandos abaixo na raiz do repositório para revisar as alterações.

- `git status --short` lista arquivos modificados, preparados para commit e não rastreados.
- `git diff --check` aponta problemas de espaço em branco nas alterações ainda fora da área de preparação.
- `git diff --cached --check` faz a mesma verificação nas alterações preparadas para commit.

## Estilo de código e testes

Use inglês nos identificadores, comentários de código, configurações técnicas, contratos da API, nomes de tabelas e nomes de testes. Siga as convenções de nomenclatura e formatação da linguagem ou do framework adotado. Documente o comando de formatação junto aos de compilação e teste. Ao configurar os testes, registre o padrão de nomes dos arquivos e a meta de cobertura, se houver. Dê aos testes nomes que descrevam o comportamento verificado.

## Commits e pull requests

O histórico do Git começa com `first commit`. Use títulos curtos e descritivos, como `Documentar a configuração local`. Nos pull requests, resuma a mudança e informe as verificações executadas. Inclua o link da issue relacionada, se houver, e capturas de tela quando a mudança afetar a interface.
