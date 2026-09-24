# PLENO — Manual de Ambientação (M5.4)

> Complemento do GDD (seções 1.1.1, 8, 11) e do documento de arte.
> **Problema que este documento resolve:** a mesa, a roleta e as fichas somam "simulador de cassino". Ambientação é outra coisa, e vem principalmente do que **não** é a mesa.

---

## 1. Diagnóstico: por que ainda parece um cassino

Um cassino e um porão de cobrança podem ter exatamente os mesmos móveis. O que os separa não é o cenário, é isto:

| Cassino | PLENO |
|---|---|
| Lugar amplo, você circula | **Você está preso numa cadeira**, e a câmera nunca deixa esquecer |
| Claro, colorido, convidativo | Uma luz só, e o resto é escuridão |
| Barulhento e alegre | Silêncio, com sons isolados e altos demais |
| Ninguém olha para você | **Alguém olha o tempo todo** |
| Sem tempo morto | O tempo morto é o desconforto |
| Você pode sair quando quiser | Sair não é uma opção oferecida |

Então a pergunta certa não é "que móveis faltam?", e sim: **o jogador sente que está preso, observado e devendo?**

### 1.1 Os cinco níveis, em ordem de custo-benefício

Para um dev solo, a ambientação se compra nesta ordem. Os quatro primeiros são baratos e valem mais que o quinto.

1. **Som** — o mais barato e o que mais entrega. Sozinho muda o gênero percebido.
2. **Luz e névoa** — você já tem os estágios; falta o comportamento (falha, zumbido, escuridão de verdade).
3. **Comportamento do Crupiê** — um boneco simples que *reage na hora certa* assusta mais que um personagem bem modelado parado.
4. **Câmera e ritmo** — enquadramento preso, tempos mortos, esperas.
5. **Cenário** — o mais caro e o menos decisivo. Vem por último.

Se você fizer 1 a 4 com o greybox ainda cinza, o jogo já para de parecer cassino.

---

## 2. A ficção mínima (contada sem texto)

**A Casa** cobra dívidas em mesas de roleta. Ninguém explica as regras e ninguém explica o que acontece se você perder. O jogador não é herói: é devedor.

Três coisas precisam ficar claras sem uma linha de diálogo:

| O que o jogador precisa entender | Como o jogo conta |
|---|---|
| "Eu não escolhi estar aqui" | Abertura: tela preta, som de porta trancando atrás, luz acende sobre a mesa. Sem menu, sem logo antes disso |
| "Não dá para sair" | Não existe botão de levantar. A vista da porta existe, e a porta está fechada |
| "Isso vai fundo" | Entre Antes, você **desce**: a mesa some, a câmera atravessa um corredor escuro e senta em outra mesa mais abaixo |

**Regra de ouro da escrita:** o Crupiê nunca fala. Toda informação que precisaria de fala vira **bilhete escrito à mão** empurrado sobre o pano, com no máximo 8 palavras.

---

## 3. Os espaços (blockout com medidas)

Modele em blocos cinza primeiro, no Blender, **com estas medidas**, e só depois texture. A câmera do jogador fica sempre a 1,20 m do chão e a 0,55 m da borda da mesa.

### 3.1 Ante 1 — O Porão

- **Sala:** 5,0 × 4,0 m, pé-direito 2,4 m. Pequena de propósito: teto baixo sufoca.
- **Mesa:** 1,8 × 1,0 m, altura 0,78 m, no centro deslocada 0,5 m para trás.
- **Lâmpada:** 1,9 m do chão, 0,3 m à frente do centro da roda. É a única luz do estágio 0.
- **Elementos:** parede de tijolo úmido, um cano atravessando o teto (pinga em ritmo), caixas de bebida empilhadas num canto, uma porta de aço fechada na parede do fundo, atrás do Crupiê.
- **Detalhe que vende o lugar:** marcas de giz na parede contando outras partidas. Muitas.

### 3.2 Ante 2 — A Sala de Máquinas

- **Sala:** 7,0 × 5,0 m, pé-direito 3,5 m.
- **Elementos:** caldeira rebitada ao fundo que **ronca a cada giro**, correntes penduradas do teto, manômetros na parede, vapor saindo de um cano lateral em intervalos irregulares.
- **Luz:** a lâmpada agora tem uma gaiola de metal, e a sombra dessa gaiola cai sobre a mesa.

### 3.3 Ante 3 — O Salão Vazio

- **Sala:** 14 × 10 m, pé-direito 6,0 m. Grande demais: agora o medo é o espaço vazio.
- **Elementos:** dezenas de mesas cobertas por lençóis ao redor, cadeiras viradas, um lustre apagado bem acima, carpete comido por mofo.
- **Luz:** só a sua mesa é iluminada. O resto do salão só aparece no estágio 4.
- **Detalhe:** quando a luz do estágio 2 acende, dá para ver que **as outras mesas têm cadeiras ocupadas**. No estágio 3 elas estão vazias. Nunca comente isso.

### 3.4 A jaula

- Não existe controle de movimento. O mouse gira a cabeça no máximo **±35° na horizontal e ±25° na vertical**.
- Se o jogador tentar olhar para trás por mais de 2 segundos, a cabeça volta sozinha para a mesa, devagar.
- A porta está sempre no campo de visão quando ele olha para o fundo. Ela nunca abre, até o fim da demo.

---

## 4. As transições (o que mais está faltando hoje)

Hoje o jogo provavelmente troca de mesa instantaneamente. **É aí que o cassino aparece**, porque cassino é uma sucessão de rodadas sem consequência. Substitua por isto:

| Momento | O que acontece | Duração |
|---|---|---|
| Início da run | Preto. Fechadura tranca. Luz acende sobre a mesa. O Crupiê empurra as fichas para você | 4 s |
| Mesa vencida | Silêncio. A luz estabiliza. O Crupiê empurra a maleta (loja) | 2 s |
| Entre mesas do mesmo Ante | O Crupiê recolhe tudo com o rodo, limpa o pano com a mão e recoloca as fichas | 3 s |
| **Entre Antes (a descida)** | A luz apaga. Passos. A câmera atravessa um corredor escuro em primeira pessoa, sem controle do jogador, por 6 a 8 s, e senta numa mesa igual, mais funda | 8 s |
| Chefe entrando | A luz apaga por 0,8 s. Quando volta, **outra figura** está sentada na frente | 1,5 s |
| Derrota | Corte seco para preto no meio do som. Estatísticas escritas à mão sob a luz de um isqueiro | — |

**Importante:** a descida entre Antes pode ser pulada com uma tecla depois da primeira vez. Atmosfera que o jogador não pode pular vira irritação na décima run.

---

## 5. O Crupiê: presença é comportamento, não modelo

Um boneco low-poly que **reage no tempo certo** intimida mais que um personagem detalhado parado. Especifique o comportamento antes de modelar.

### 5.1 Máquina de estados de presença

| Estado | Comportamento | Gatilho |
|---|---|---|
| `Idle` | Respiração lenta, ombros sobem 2 cm a cada 4 s. Micro-movimento aleatório a cada 6 a 12 s | padrão |
| `Watching` | A cabeça acompanha **onde o cursor do jogador está** no pano, com atraso de 0,4 s | jogador movendo fichas |
| `Impatient` | Uma batida seca de dedo na mesa aos 20 s parado, duas batidas aos 45 s. Nunca há timer na tela | jogador parado apostando |
| `Serving` | Estende a mão, segura 1 s, solta a bola | jogador aperta Girar |
| `Collecting` | Puxa as fichas perdidas com um rodo de madeira, devagar, olhando para você | giro sem vitória |
| `Recoil` | Recua 10 cm na cadeira, mãos saem da mesa | giro no estágio de luz 1 ou 2 |
| `Standing` | **Levanta-se**, e a partir daí fica de pé, fora do foco da luz | estágio 3 |
| `Gone` | A cadeira está vazia. Não há animação de saída: ele simplesmente não está mais lá | estágio 4 |

O `Impatient` e o `Watching` são os dois que mais mudam a sensação, e são os mais baratos: uma rotação de cabeça e uma animação de dedo.

### 5.2 Modelo

- Rosto **nunca** visível: aba de chapéu, capuz ou contraluz. Isso economiza a parte mais cara da modelagem e aumenta o desconforto.
- O que precisa ser bem feito: **as mãos** (com luva) e a **silhueta dos ombros**. É só isso que o jogador vê com clareza.
- Sem rig facial, sem blendshapes. Um rig humanoide simples com 6 a 8 animações resolve a demo.

### 5.3 Os seis Crupiês-chefe

Cada um herda o comportamento padrão e acrescenta **uma entrada, um objeto, um tique e uma mudança de ambiente**. Nenhum precisa de modelo novo do zero: troque silhueta (chapéu, casaco, altura) e mantenha o mesmo rig.

| Crupiê | Entrada | Objeto na mesa | Tique | Ambiente |
|---|---|---|---|---|
| O Duplo Zero | Aparece rindo sem som | Crava uma segunda bolinha verde na roda | Ombros tremem, sem áudio | Luz ganha um pulso lento |
| A Névoa | Já está lá quando a luz volta, mais perto que o normal | Incenso aceso | Respiração pesada e lenta | Névoa densa sobe da mesa |
| O Contador | Entra contando fichas em voz baixa (murmúrio ininteligível) | Ábaco | Estala os dedos a cada ficha que você coloca | Zumbido elétrico mais alto |
| Mão Pesada | A cadeira range muito ao receber o peso | Mão enfaixada sobre a borda da roda | Batida seca se você tentar empurrar | Luz balança como se alguém tivesse esbarrado |
| O Daltônico | Entra de costas e se vira devagar | Óculos de lentes cinza sobre o pano | Inclina a cabeça 20° olhando você | Cores do cômodo dessaturam 40% |
| O Relojoeiro | Ouve-se o tique-taque antes de a luz acender | Relógio de bolso aberto | Confere o relógio antes de cada giro | Tique-taque entra na mixagem e some nos outros sons |

---

## 6. Áudio: a lista concreta

Esta é a parte que muda o gênero percebido e custa pouco. Sons de biblioteca livre (Freesound, com atenção à licença) resolvem quase tudo; o que falta você grava com o celular numa garagem.

### 6.1 Camadas permanentes (loop)

| Som | Papel | Nota |
|---|---|---|
| Room tone do cômodo | Base | Um por Ante. Sem isso, tudo soa "morto de estúdio" |
| Zumbido da lâmpada | Tensão constante | Sobe 3 dB quando a luz falha |
| Pingo de cano (Ante 1) | Marca o tempo | Intervalo irregular, entre 3 e 7 s |
| Caldeira (Ante 2) | Pulso | Ronca junto com cada giro |
| Respiração do Crupiê | Presença | Bem baixa, só perceptível no silêncio |
| Drone grave | Escala com o Mult | 4 camadas que entram conforme o Mult do giro sobe |

### 6.2 Efeitos pontuais (3 a 5 variações de cada)

Bola no prato (loop com pitch pela velocidade) · queda da bola na casa · quique no defletor · ficha colocada · pilha de fichas · rodo puxando fichas · batida de dedo na mesa · papel do bilhete deslizando · fechadura do cofre catracando · tampa do cofre abrindo · palhetas do placar virando · tique elétrico do Nixie · lâmpada falhando · cadeira rangendo · porta trancando · passos no corredor da descida.

### 6.3 Regras de mixagem (é aqui que mora o medo)

- **Silêncio é instrumento.** Nos últimos 0,8 s do giro, abafe tudo menos a bola.
- **Dinâmica exagerada:** o ambiente é muito baixo e os efeitos pontuais são altos. Assim o jogador sobe o volume e é pego pelos sons secos.
- **Reverb por sala:** porão curto e abafado, sala de máquinas metálica, salão vazio com cauda longa. O mesmo som de ficha em três salas diferentes já conta onde você está.
- **Estágio 4:** 1 segundo de silêncio absoluto antes do estrondo.
- Nada de música durante a mesa. Música só na tela de fim de run.

---

## 7. O ritmo de uma mesa (mapa de tempos)

Ambientação é ritmo. Um cassino não tem pausa; o PLENO tem.

| t | Evento | Sensação |
|---|---|---|
| 0 s | Fichas empurradas para você | "começou" |
| 0–? | Você aposta. O Crupiê acompanha o cursor | observado |
| +20 s parado | Uma batida de dedo | pressão sem timer |
| Girar | Ele estende a mão e **espera 1 s** antes de soltar | desconforto |
| 3,5 s | Bola girando, ambiente abafa | tensão |
| Bola para | **1 s de nada.** Ninguém pontua ainda | o pico |
| Empurrão | Só se você escolher. Um "tec" seco | decisão |
| Pontuação | Nixies, placar, fichas reagindo | recompensa |
| Sem vitória | O rodo puxa as fichas, devagar, com ele olhando | punição |

O momento mais importante do jogo inteiro é **o segundo de silêncio depois que a bola para**. Não preencha esse segundo com nada.

---

## 8. Ordem de produção

| # | Tarefa | Onde | Custo |
|---|---|---|---|
| 1 | Sistema de ambiente e áudio (prompt A) | Claude Code | baixo |
| 2 | Comportamento do Crupiê com um boneco cinza (prompt B) | Claude Code | baixo |
| 3 | Transições, descida entre Antes e a jaula da câmera (prompt C) | Claude Code | baixo |
| 4 | Blockout cinza das 3 salas com as medidas da seção 3 | Blender | médio |
| 5 | Texturas e adereços do Ante 1 | Blender | médio |
| 6 | Modelo e animações do Crupiê padrão | Blender | alto |
| 7 | Variações de silhueta dos 6 chefes | Blender | médio |
| 8 | Salas 2 e 3 | Blender | alto |

Faça 1, 2 e 3 antes de abrir o Blender. Eles já respondem se a ambientação funciona.

---

## 9. Teste de ambientação: o print mudo

Antes de investir nos itens 4 em diante:

1. Tire um print do jogo **sem a interface**.
2. Mostre para 3 pessoas que não conhecem o projeto e pergunte: **"que lugar é esse e como você se sente?"**
3. Se a resposta for "um cassino" ou "uma mesa de jogo", a ambientação ainda não existe. Se for "um porão", "um interrogatório", "um lugar ruim", funcionou.

Repita o mesmo teste com **áudio e sem imagem**: toque 20 segundos de gameplay com a tela apagada e faça a mesma pergunta. Se o som sozinho não entregar o lugar, volte para a seção 6 antes de modelar qualquer coisa.

---

## 10. Prompts do Claude Code

### Prompt A — Sistema de ambiente e áudio

```
Leia Docs/PLENO_Ambientacao_M5-4.md (seções 6 e 7) e Docs/GDD_PLENO.md (seção 11.7) antes
de começar. Implemente o sistema de ambiente e áudio. Use placeholders de som: gere ou use
clipes curtos de teste, o importante é a ARQUITETURA e os gatilhos, não os arquivos finais.

1. AmbienceProfile (ScriptableObject, um por Ante)
   Room tone, lista de loops ambientes com intervalo e variação, parâmetros de reverb da
   sala, curva do drone por faixa de Mult, e cor/intensidade da luz (referenciando o
   documento de arte).

2. AmbienceDirector (Pleno.Presentation)
   Toca as camadas do perfil ativo, faz crossfade ao trocar de Ante, e controla a
   "ducking": nos últimos 0,8 s do giro abaixa tudo menos a bola em -18 dB, e devolve
   depois da pontuação. Usa um AudioMixer com grupos: Ambience, Foley, Ball, UI, Sting.

3. BallAudio
   Loop do chocalho com pitch e volume ligados à velocidade angular da bola vinda do
   BallAnimator. Um-shots nos quiques e na queda final. Nada de som aleatório fora da
   seed: as variações escolhidas vêm da seed do giro.

4. FoleyLibrary
   Um registro de eventos nomeados ("chip_place", "rake_pull", "note_slide",
   "vault_ratchet", "nixie_tick", "flap_clack", "finger_tap", "lamp_flicker") com 3 a 5
   variações cada e seleção sem repetir a anterior. Todo o resto do jogo dispara som por
   NOME, nunca referenciando AudioClip direto.

5. Silêncio como evento
   Um método Silence(float seconds) que corta tudo menos o que for marcado como essencial.
   Usado no segundo depois que a bola para e no estágio 4 de luz.

6. Integração com o ScorePlaybackController
   Cada ScoreStep dispara o foley da sua origem. Passo sem som mapeado gera aviso no
   console, nunca exceção.

Verificação: rode uma mesa inteira e confirme que existe o segundo de silêncio depois que
a bola para; troque de Ante e confirme o crossfade; rode a mesma seed duas vezes e confirme
que as variações de som escolhidas são idênticas.
```

### Prompt B — Presença do Crupiê

```
Leia Docs/PLENO_Ambientacao_M5-4.md (seção 5). Implemente a presença do Crupiê. Use uma
cápsula cinza com duas mãos cúbicas como placeholder: o objetivo é o COMPORTAMENTO.

1. DealerPresence (Pleno.Presentation), máquina de estados com os estados da tabela 5.1:
   Idle, Watching, Impatient, Serving, Collecting, Recoil, Standing, Gone.
   Os gatilhos vêm de eventos que a camada de jogo já emite (aposta colocada, giro pedido,
   giro sem vitória, estágio de luz alterado). O Core não sabe que isso existe.

2. Watching: a cabeça acompanha a posição do cursor sobre o pano com atraso de 0,4 s e
   limite de rotação de 45 graus. Quando o cursor sai do pano, volta ao centro em 1,5 s.

3. Impatient: aos 20 segundos sem o jogador colocar ficha nem girar, uma batida de dedo
   (evento de foley "finger_tap"); aos 45, duas. Reinicia a contagem a cada ação do
   jogador. NÃO mostre nenhum timer na tela.

4. DealerVariant (ScriptableObject): perfil por crupiê com altura, deslocamento de
   silhueta, objeto que ele coloca na mesa, tique (animação + som + intervalo) e o efeito
   de ambiente associado. Crie os 6 chefes da seção 5.3 como assets, usando placeholders.

5. Entrada do chefe: apaga a luz por 0,8 s, troca o variante, acende. Uma corrotina só,
   cancelável se a cena for descarregada.

Verificação: jogue uma mesa e confirme que a cabeça te acompanha; fique parado 50 segundos
e confirme as duas batidas; force os estágios de luz 3 e 4 e confirme que ele levanta e
depois some. Mostre um GIF do Watching e do Impatient.
```

### Prompt C — Transições, descida e a jaula

```
Leia Docs/PLENO_Ambientacao_M5-4.md (seções 3.4 e 4). Implemente as transições e a câmera
presa. Sem assets novos: use o blockout atual e corredores de caixas cinza.

1. CameraCage: o mouse gira a cabeça em no máximo ±35 graus na horizontal e ±25 na
   vertical, com suavização. Olhando para fora do foco por mais de 2 s, a câmera volta
   sozinha para a mesa em 1,2 s. Nenhum controle de movimento existe.

2. SceneFlowDirector: sequências roteirizadas, cada uma cancelável, com a duração da
   tabela da seção 4:
   - RunStart: preto, som de fechadura, luz acende, fichas empurradas
   - MesaWon: silêncio, luz estabiliza, maleta empurrada
   - MesaNext: recolhimento com rodo e recolocação das fichas
   - AnteDescent: luz apaga, passos, travelling em primeira pessoa por um corredor escuro
     de 6 a 8 s, senta na mesa do Ante seguinte
   - BossEntry: escuro por 0,8 s e troca do variante do crupiê
   - RunLost: corte seco para preto no meio do som

3. Pulo de transição: qualquer sequência pode ser pulada com Espaço DEPOIS de ter sido
   vista uma vez (registre no perfil do jogador o que já foi visto). A primeira vez nunca
   é pulável.

4. As sequências não podem travar o jogo se forem interrompidas: se a cena for trocada no
   meio, o estado final é aplicado imediatamente. Teste isso de propósito.

Verificação: jogue do começo até o Ante 2 e confirme a descida; repita e confirme que dá
para pular; force uma troca de cena no meio de cada sequência e confirme que nada fica
preso num estado intermediário.
```

---

*Documento vivo. Depois do teste do print mudo, revisar as seções 5 e 6 com o que os testadores responderam.*
