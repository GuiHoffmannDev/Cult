# PLENO

Roguelike deckbuilder de roleta, 2D, Unity 6 (6000.6.2f1) com URP 2D. A roda é o baralho: o
jogador aposta, gira, empurra a bola e reconstrói a roda casa por casa.

Leia antes de mexer em regras ou visual:
- `Docs/GDD_PLENO.md`: regras, conteúdo, economia, retenção.
- `Docs/PLENO_Migracao_2D.md`: layout 2D, paleta, direção de arte. Substitui a seção 11 do GDD.
- `Docs/PLENO_Ambientacao_M5-4.md`: presença do Crupiê, áudio, ritmo.
- `Docs/PLENO_Prompt_ZeroAbsoluto.md`: o plano por fases e o **resumo de regras**, que é a fonte
  da verdade para o Core. Onde ele e o GDD divergirem, pergunte.

## Arquitetura (inegociável)

| Assembly | Pasta | Pode referenciar | Conteúdo |
|---|---|---|---|
| `Pleno.Core` | `Assets/_Pleno/Scripts/Core` | nada | Regras puras. `noEngineReferences: true` |
| `Pleno.Data` | `Assets/_Pleno/Scripts/Data` | Core | ScriptableObjects de conteúdo (nome, ícone, texto, preço) |
| `Pleno.Game` | `Assets/_Pleno/Scripts/Game` | Core, Data, Newtonsoft | Máquina de estados da run, save, seed |
| `Pleno.Presentation` | `Assets/_Pleno/Scripts/Presentation` | Core, Data, Game, pacotes visuais | Tudo que é visual e sonoro |
| `Pleno.Sim` | `Assets/_Pleno/Scripts/Sim` | Core, Data, Game | Simulador (só Editor, headless via `-executeMethod`) |
| `Pleno.Tests` | `Assets/_Pleno/Tests/EditMode` | Core, Data, Game | Testes NUnit EditMode |

Regras:
1. **Pleno.Core não conhece a Unity.** Nada de `UnityEngine`, nem `Vector2`, nem `Mathf`, nem
   `Debug`, nem `Random`. Se precisar de algo da Unity, a lógica está na camada errada.
2. **A casa sorteada é decidida pelo Core antes de qualquer animação.** A física é falsa: a
   animação é resolvida para terminar na casa que o Core já escolheu.
3. **Proibido `UnityEngine.Random` e `System.Random` em qualquer lugar**, inclusive em ruído de
   animação e de som. Todo o RNG vem do `SeededRng` (PCG32), com um stream nomeado por sistema:
   `wheel`, `shop`, `pack`, `relic`, `boss`, `luck`, `vault`. Ruído visual usa um stream derivado
   da seed, nunca um desses sete (animar não pode mudar o próximo giro).
4. **Calcular primeiro, animar depois.** A pontuação vira uma lista de `ScoreStep`; a tela só toca
   a lista. A Presentation nunca calcula pontos.
5. **Nenhum valor de balanceamento espalhado pelo código.** Números de regra (pontos, mult, metas,
   preços, chances) moram num único arquivo de balanceamento do Core.
6. **Nenhuma cor escrita no código.** Cores moram no `GraphicsProfile` (ScriptableObject).
   `Color.white` e `Color.clear` são permitidos como valores neutros.
7. Pontuação em `double`. O resultado do giro é `floor(Pontos × Mult)`.

As regras 1, 3 e 6 são verificadas por `ArchitectureGuardTests`. Se um desses testes falhar,
corrija o código, nunca o teste.

## Convenções

- Código, nomes de tipos e comentários técnicos em inglês. Termos do domínio podem ficar em
  português quando não houver tradução boa (`Mesa`, `Ante`, `Pleno`).
- Texto de UI em PT-BR, sempre via tabela de localização (EN vem na Fase 11). Nunca string de UI
  literal no código.
- Conteúdo (amuletos, cartas, chefes) tem id estável em kebab-case (`teorema`, `luva-de-seda`);
  saves e logs usam o id, nunca o nome exibido.
- Arte 1080p, 100 pixels por unidade; a câmera ortográfica tem `size = 5.4`.

## Processo

- Uma PR por fase (`Docs/PLENO_Prompt_ZeroAbsoluto.md`); só avançar com aprovação.
- Relatório de cada fase: o que foi criado, o que ficou pendente, resultado dos testes, print ou GIF.
- **Se uma regra não estiver no resumo nem no GDD, pare e pergunte. Não invente regra.**
- Se o teste de regressão do exemplo 1.683 (GDD 3.4) divergir, pare e pergunte antes de mexer na regra.

Decisões já tomadas:
- Metas do Ante 3: 800 / 1200 / 1600 (o resumo vence a calibração antiga do GDD 4.1).
- O Mult do giro começa em 0: o exemplo 1.683 exige 1 + 1 + 5 + 10 = 17.
- O 00 soma 36 nos Pontos, igual ao 0.
- Vizinhos cobre 5 posições físicas em volta da casa clicada na pista (âncora = Id da casa).
  Um número repetido em outro ponto da roda não conta; se a âncora for apagada, a aposta não vence.
- Giro sem vitória marca 0 e não chama os amuletos no passo 3, mas o pós-giro acontece:
  Dourada paga, Sortuda sorteia o $ e o Vidro pode quebrar.
- Aço acumula: a casa sorteada e cada vizinha imediata de Aço dão ×1,5 cada.
- Casa Rachada: a bola sorteada nela salta uma vez para uma vizinha aleatória (stream `wheel`)
  e fica lá, mesmo que a vizinha também seja Rachada. Só age no sorteio, não no empurrão.
  Ao quebrar, a casa perde o Vidro e mantém a edição.
- Colecionador conta números desde que entrou; o número do giro atual vale a partir do próximo.
- Sortuda: dois sorteios independentes (Mult e $), feitos sempre na mesma ordem.
- Juros incidem sobre o $ que o jogador tinha ao bater a meta, antes da recompensa e dos giros.
- Preço de amuleto = piso da faixa da raridade (C $4, I $6, R $8), em `Balance.Shop.RelicPrices`.
  Preços moram no Balance (o simulador precisa deles sem Unity), não nos ScriptableObjects.
- Loja: a raridade é sorteada só entre raridades com amuleto disponível (proporção mantida).
  Nunca oferece amuleto possuído nem o mesmo duas vezes (vale para o Pacote de Amuletos).
- Espaço de consumível: 50% Tábua, 50% Carta, depois item uniforme; os dois espaços são independentes.
- Pacote à venda: uniforme entre os 3. Opções dentro de um pacote nunca repetem.
- Reroll troca amuletos e consumíveis; o pacote fica.
- Pacote: Tábua e Carta escolhidas são usadas na hora; amuleto vai para o espaço livre; dá para pular.
- Contador e Daltônico: aposta anulada conta como perdida (não ativa amuletos; se era a única, giro = 0).
- Duplo Zero: 00 temporário oposto ao primeiro 0; sai no fim da mesa. Não é alvo nem origem de carta.
- Edição de amuleto (Fortuna): mesmos valores da edição de casa, logo depois do amuleto, em todo
  giro vencedor. A Fortuna só sorteia entre amuletos sem edição.
- Artesão faz cópia completa (aprimoramento e edição). Roda tem no máximo 48 casas (Artesão).
- Aprimorar uma Casa Rachada a conserta. O Pintor não aceita casa verde.
- Girar exige pelo menos 1 ficha no pano.

## Rodar os testes

Com o Editor aberto (o caminho normal): Unity MCP, `run_tests` em modo `EditMode`.

Em batchmode (só com o Editor **fechado**, porque o Unity trava o projeto aberto):

```sh
"/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform EditMode \
  -testResults "$PWD/TestResults/editmode.xml" -logFile -
```

Não use `-quit` junto com `-runTests`. O código de saída é diferente de 0 se algum teste falhar;
os detalhes ficam em `TestResults/editmode.xml`.
