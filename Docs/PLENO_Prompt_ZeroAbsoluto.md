# PLENO 2D — Prompt de construção do zero (Core incluído)

## Antes de colar

1. Crie o projeto no Unity Hub: **Unity 6 LTS**, template **Universal 2D**, nome `Pleno`.
2. Copie para `Docs/` os documentos: `GDD_PLENO.md`, `PLENO_Migracao_2D.md` e `PLENO_Ambientacao_M5-4.md`.
3. `git init`.
4. Opcional, mas recomendado: copie `Tests/` do projeto antigo para `~/pleno-legado-tests/`. São a especificação executável das regras; o prompt tem um passo para usá-las como referência.
5. Arquive o repositório antigo em vez de apagar.

---

## O prompt

```
Você vai construir o PLENO do zero, em um projeto Unity 6 recém-criado com o template
Universal 2D. Não há código anterior a reaproveitar: tudo será escrito agora.

Leia antes de começar: Docs/GDD_PLENO.md, Docs/PLENO_Migracao_2D.md e
Docs/PLENO_Ambientacao_M5-4.md. O resumo abaixo é a fonte da verdade para as fases 1 e 2;
onde ele e os documentos divergirem, pergunte antes de decidir.

═══════════════════════════════════════════════════════════════════
RESUMO DAS REGRAS
═══════════════════════════════════════════════════════════════════

Roguelike deckbuilder de roleta, 2D. Uma run tem 3 Antes; cada Ante tem Mesa Pequena, Mesa
Grande e Mesa do Chefe. Em cada mesa o jogador tem 4 giros, 5 fichas por giro, e precisa
bater uma meta de pontos. Entre mesas há uma loja.

A RODA
- Lista ORDENADA de casas; a ordem física importa (vizinhos e empurrão).
- Casa: numero (0-36; 00 = -1), cor (Vermelho/Preto/Verde), aprimoramento
  (Nenhum/Dourada/Vidro/Aço/Bônus/Mult/Magnética/Sortuda), edição
  (Nenhuma/Polida/Holográfica/Policromática), peso (base 1.0).
- Roda europeia: 37 casas na ordem física real. Americana: 38, com 00.
- A roda pode ganhar e perder casas (mínimo 18). Números repetidos são permitidos.

APOSTAS (valores base, nível 1)
  tipo                         casas  pontos  mult  +pontos/nivel  +mult/nivel
  Chance simples                 18      5      1       +10           +1
  Dúzia / Coluna                 12     10      1       +15           +1
  Linha (6)                       6     15      2       +15           +1
  Vizinhos (número + 2 de cada lado NA RODA)  5   20   2   +20        +2
  Quadra                          4     20      2       +20           +2
  Rua                             3     25      3       +20           +2
  Cavalo                          2     30      3       +25           +2
  Pleno                           1     50      5       +30           +3
- Fichas empilhadas multiplicam os PONTOS da aposta, não o Mult.

PONTUAÇÃO — ordem de resolução, determinística
1. Apostas vencedoras, na ordem em que foram colocadas:
   Pontos += PontosBase(tipo, nível) * fichas ; Mult += MultBase(tipo, nível)
2. Casa sorteada: Pontos += valor do número (o 0 soma 36); depois aprimoramento; depois edição.
3. Amuletos, da esquerda para a direita.
4. Resultado = floor(Pontos * Mult), somado ao total da mesa.
5. Pós-giro: dinheiro, quebra de Vidro, atualização de amuletos que escalam.
REGRA DO ZERO: caindo 0 ou 00, TODAS as apostas externas perdem (chances simples, dúzias e
colunas). Apostas internas que incluam o zero pontuam normalmente.
Giro sem vitória marca 0 pontos (alguns amuletos ativam nisso).

TESTE DE REGRESSÃO OBRIGATÓRIO
Apostas: 2 fichas no Vermelho, 2 na 2ª Dúzia, 1 no Pleno 19. Cai o 19. Com o amuleto
"Teorema" (+10 Mult em número primo) equipado, o resultado tem de ser exatamente 1.683.
  Vermelho 5*2=10 | Dúzia 10*2=20 (total 30) | Pleno 50 (total 80) | número 19 (total 99)
  Mult: 1 + 2 + 5 = 8? NÃO: chance simples 1, dúzia 1, pleno 5, e o Teorema +10 → 17.
  99 * 17 = 1683.
  Se o seu cálculo divergir, PARE e me pergunte antes de "ajustar" a regra.

APRIMORAMENTOS DE CASA
Dourada +$3 | Vidro x2 Mult, 1 em 4 de quebrar (vira Casa Rachada: a bola salta para uma
vizinha) | Aço x1,5 Mult se a bola cair nela ou numa vizinha | Bônus +30 Pontos |
Mult +4 Mult | Magnética peso x2 | Sortuda 1/5 +20 Mult, 1/15 +$20.
EDIÇÕES: Polida +50 Pontos | Holográfica +10 Mult | Policromática x1,5 Mult.

RECURSOS POR MESA
4 giros | 5 fichas por giro (não acumulam) | 2 empurrões (movem a bola 1 casa na ordem
física da roda, DEPOIS que ela para).

METAS E ECONOMIA
Ante 1: 100 / 150 / 200 | Ante 2: 300 / 450 / 600 | Ante 3: 800 / 1200 / 1600.
$ inicial 4. Recompensa 3/4/5 por mesa + $1 por giro não usado. Juros +$1 a cada $5, teto
+$5. Venda = metade do preço, mínimo $1. Loja: 2 amuletos, 2 consumíveis, 1 pacote; reroll
$5, +$1 a cada reroll na mesma loja. 5 espaços de amuleto (a ORDEM importa), 2 de consumível.

CONTEÚDO (detalhado no GDD, seções 6, 7 e 8)
40 amuletos, 12 Cartas da Casa, 8 Tábuas, 3 pacotes, 6 chefes, 3 rodas iniciais.

═══════════════════════════════════════════════════════════════════
ARQUITETURA (inegociável)
═══════════════════════════════════════════════════════════════════

- Pleno.Core — regras puras. asmdef com noEngineReferences = true. Nenhuma referência à
  Unity, nem para vetor, nem para Random, nem para Debug.
- Pleno.Data — ScriptableObjects de conteúdo (nome, ícone, texto, preço).
- Pleno.Game — máquina de estados da run, save, seed.
- Pleno.Presentation — tudo que é visual.
- Pleno.Tests (EditMode) e Pleno.Sim (simulador).
Regras:
- A casa sorteada é decidida pelo Core ANTES de qualquer animação.
- Proibido UnityEngine.Random em qualquer lugar, inclusive em ruído de animação e de som.
- Calcular primeiro, animar depois: a pontuação vira uma lista de ScoreSteps que a tela toca.
- Nenhum valor de balanceamento espalhado pelo código: tudo num arquivo de balanceamento.
- Nenhuma cor escrita no código: tudo num GraphicsProfile.

═══════════════════════════════════════════════════════════════════
FASES — uma PR por fase, só avance depois da minha aprovação
═══════════════════════════════════════════════════════════════════

FASE 0 — Fundação
Estrutura Assets/_Pleno/{Scripts,Data,Prefabs,Scenes,Art,Audio,Settings}; os cinco asmdefs;
.gitignore da Unity; Git LFS para png/wav/ogg; pacotes 2D Animation, 2D PSD Importer,
TextMeshPro, PrimeTween, Newtonsoft Json; cena Game.unity com câmera ortográfica e Canvas
1920x1080 Scale With Screen Size; CLAUDE.md com as regras de arquitetura acima e o comando
de rodar os testes em batchmode.

FASE 1 — Core: roda, apostas e pontuação
- SeededRng (PCG32) com streams nomeados derivados de uma seed de 8 caracteres:
  wheel, shop, pack, relic, boss, luck, vault.
- Modelo: Pocket, Wheel (ordenada, vizinhos circulares, add/remove/clone), BetType (os 8),
  PlacedBet, RelicInstance com contadores serializáveis.
- Layout físico real da roda europeia e da americana.
- BetResolver: dado um Pocket, quais apostas vencem (com a regra do zero e Vizinhos pela
  ordem da roda).
- WheelRng: sorteio ponderado por peso.
- ScoringPipeline na ordem descrita, retornando SpinResult com a lista de ScoreSteps
  (Kind, SourceId, PontosAdd, MultAdd, MultX, MoneyDelta, snapshots para a UI). Use double.
- IRelicEffect + registro por atributo; implemente 10 amuletos: Ficha Gasta, Rosa Rubra,
  Arranha-céu, Teorema, Mira Laser, Combo Triplo, Martingale, Colecionador, Luva de Seda,
  Ímã de Neodímio.
- TESTES: o de regressão do 1.683; regra do zero; Vizinhos na virada de índice; clone e
  remoção de casas; sorteio ponderado com distribuição esperada em 100 mil amostras;
  determinismo (mesma seed = mesma sequência).

FASE 2 — Core: run, loja, chefes e save
RunState e MesaState, metas e economia acima, geração de loja e pacotes (stream próprio),
os 6 chefes como IBossRule, 12 Cartas da Casa, 8 Tábuas, 3 rodas iniciais, save em JSON
incluindo o estado dos streams de RNG. Testes para economia (juros, recompensa, venda) e
para cada regra de chefe.

FASE 3 — Simulador
Pleno.Sim com 4 bots (Aleatório, Cobertura, Sniper, Guloso). O Guloso e o Sniper DEVEM
saber usar empurrões e Cartas da Casa, senão a medição do arquétipo é inútil. 800 runs por
bot, saída em CSV: taxa de vitória por ante, pontuação média e p90, taxa de compra por
amuleto. Me mostre a tabela.

FASE 4 — Layout 2D jogável (placeholders)
A tela da seção 2 de PLENO_Migracao_2D.md com retângulos coloridos: Crupiê no topo
(520x640), roda em elipse ao centro (560x360), pano 3x12 + externas + pista de Vizinhos
embaixo, 5 amuletos, 2 consumíveis, meta, total da mesa, Pontos x Mult, fichas, botão Girar.
Clique esquerdo coloca ficha, direito remove, Shift+clique coloca todas.
Critério: dá para jogar uma mesa inteira, mesmo sem animação.

FASE 5 — Roda 2D e bola
WheelRenderer2D (malha 2D em elipse, N de 18 a 48, cor por vertex color, aprimoramentos como
overlay, números que giram sem ficar de cabeça para baixo) e BallRenderer2D (percorre a
elipse: x = a·cosθ, y = b·sinθ; resolve o ângulo final para parar no centro da casa
sorteada; 2 ou 3 quiques com ruído vindo da seed; x1/x2/x4/pular; empurrão com arco curto).
TESTE mais importante do projeto: 1.000 combinações de seed e casa alvo, posição final a
menos de 0,5 grau do centro.

FASE 6 — Pontuação passo a passo
ScorePlaybackController toca os ScoreSteps com ritmo, destacando a origem de cada passo
(ficha, casa, amuleto). Contadores sobem junto. x1, x2 e pular.

FASE 7 — Loja e inventário
Comprar, vender, rerolar, reordenar amuletos arrastando, pacotes, Tábuas e Cartas da Casa
(as cartas aplicam clicando nas casas da roda).

FASE 8 — Fluxo da run
Seleção de mesa, regra do chefe antes da mesa, tela de fim de run (quanto faltou, maior
giro, seed), "Mais uma" em um clique, campo de seed, save e continuar. Logs: um CSV por run
com uma linha por giro, e um runs.csv com a coluna next (nova run / repetir seed / digitar
seed / fechar).

FASE 9 — Crupiê 2D e estágios de luz
DealerRig2D em partes (cabeça, tronco, 2 braços, 2 mãos, olhos, mandíbula) com placeholders
e 2D Animation. Máquina de presença: Idle, Watching (cabeça e olhos seguem o cursor com
0,4 s de atraso), Impatient (uma batida de dedo aos 20 s parado, duas aos 45 s, sem timer na
tela), Serving, Collecting, Recoil, Standing, Gone. DealerVariant2D para os 6 chefes.
Estágios de luz do GDD 17.2 em 2D: vinheta, luzes 2D, saturação do fundo, partículas; o
estágio persiste até o fim da mesa e nunca regride. Trocar sprite não pode exigir mudar código.

FASE 10 — Áudio
AmbienceDirector com perfil por Ante; FoleyLibrary por nome de evento com 3 a 5 variações
sem repetir a anterior; BallAudio com pitch pela velocidade; ducking de -18 dB nos últimos
0,8 s do giro; Silence(seconds) para o segundo de silêncio depois que a bola para e para o
estágio 4. Placeholders de som servem.

FASE 11 — Pós-processamento e opções
Vinheta, grão sutil e leve aberração, cada um com liga/desliga; volume, velocidade do giro,
resolução, idioma PT-BR/EN. Nada de baixa resolução interna: a arte é 1080p.

═══════════════════════════════════════════════════════════════════
COMO REPORTAR
═══════════════════════════════════════════════════════════════════
A cada fase: o que foi criado, o que ficou pendente, resultado dos testes e um print ou GIF.
Se alguma decisão de regra não estiver coberta pelo resumo nem pelo GDD, PARE e pergunte —
não invente regra.

Comece pela Fase 0.
```

---

## Se você copiou os testes antigos

Acrescente ao fim da Fase 1 do prompt:

```
Em ~/pleno-legado-tests/ estão os testes da versão anterior. Leia-os como ESPECIFICAÇÃO do
comportamento esperado e porte os que continuarem válidos. Onde um teste antigo contrariar
o resumo de regras deste prompt, me mostre a divergência em vez de escolher sozinho.
```

Isso recupera o valor do trabalho anterior sem carregar o código antigo.

---

## Ordem de prioridade se o tempo apertar

As fases 1, 2, 4 e 5 são o jogo. As fases 3 e 8 são o que permite calibrar e medir. As 9, 10 e 11 são a alma, mas só valem depois que o gate de diversão passar — cinco playtests ao fim da fase 8, com a pergunta "você quis jogar mais uma run?".
