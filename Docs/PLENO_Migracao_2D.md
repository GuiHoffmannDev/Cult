# PLENO — Migração para 2D (v0.4)

> **Decisão:** o jogo passa a ser 2D desenhado à mão, macabro-fofo (linguagem de Cult of the Lamb e The Binding of Isaac), com o Crupiê grande no topo encarando o jogador.
> **Arte:** gerada por IA e ajustada à mão.
> Este documento substitui a seção 11 do GDD, o `PLENO_Arte_M5.md` e o `PLENO_Pipeline_Assets.md`. O manual de ambientação continua valendo em **princípios**, com as implementações trocadas (seção 4).

---

## 1. O que sobrevive à migração

Esta é a parte boa de ter separado regras de apresentação desde o M1.

| Item | Situação |
|---|---|
| `Pleno.Core` inteiro (roda, apostas, pontuação, amuletos, chefes, loja) | **Intacto.** Não sabe se o jogo é 2D ou 3D |
| Os 267 testes | **Intactos** |
| Simulador e calibração do M2 | **Intacto** |
| Máquina de estados da run, save, seed | **Intacto** |
| GDD: loop, pontuação, amuletos, chefes, retenção (seções 1 a 9 e 17) | **Intacto** |
| Manual de ambientação: princípios, ritmo, ficção, áudio | **Intacto** — só muda como se executa |
| Fase 5.1, pipeline retrô PS1 | **Descartado.** Vira um pós-processamento 2D bem mais simples |
| Fase 5.2, malha 3D da roda | **Adaptado:** a lógica de ângulos e o solver da bola continuam; muda o desenho |
| Fase 5.3, estágios de luz | **Adaptado:** luzes 2D e escurecimento, em vez de luzes 3D |
| `PLENO_Arte_M5.md` e `PLENO_Pipeline_Assets.md` | **Substituídos** por este documento |
| Blender, Mixamo, blockout das salas | **Fora do escopo.** Não precisa mais |

**Estimativa de tempo economizado:** as fases 5.4 e 5.5 (modelagem de cenário e personagem) somavam a maior parte do M5. Some-se a isso não precisar aprender rig e animação 3D.

---

## 2. O novo enquadramento

Tela de referência **1920×1080**, arte autorada a 1080p (não é pixel art).

```
┌──────────────────────────────────────────────────────────────┐
│  ░░░░░ fundo do cômodo, escuro, com vinheta ░░░░░            │
│                    ┌───────────────┐                         │
│                    │   O CRUPIÊ    │  ← 520×640 px, animado  │
│                    │  (encarando)  │                         │
│                    └───────────────┘                         │
│   [Meta 1.200]          ╭─────────╮          [Ante 2 · Mesa] │
│   [Mesa 840]            │  RODA   │  ← elipse, 560×360 px    │
│                         ╰─────────╯                          │
│  AMULETOS ▸ [■][■][■][■][■]              CONSUMÍVEIS [□][□]  │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  PANO DE APOSTAS (3×12 + externas)  │  PISTA (vizinhos) │  │
│  └────────────────────────────────────────────────────────┘  │
│  Pontos [ 99 ] × Mult [ 17 ]      Fichas ●●●○○     [GIRAR]   │
└──────────────────────────────────────────────────────────────┘
```

- **O Crupiê é o elemento mais alto e mais visível.** É ele que carrega a ambientação sozinho, porque o cenário agora é só um fundo escuro.
- **A roda em elipse** (vista de cima em leve perspectiva), 560×360, girando de verdade. Continua gerada por código para suportar de 18 a 48 casas.
- **O pano** ocupa a faixa inferior, com cartas e fichas em 2D.
- **Fundo:** uma ilustração por Ante, escura, quase sem detalhe legível. Ele existe para dar cor e profundidade, não para ser olhado.

---

## 3. Direção de arte 2D

### 3.1 Linguagem

- **Traço:** contorno preto grosso e irregular (4 a 6 px a 1080p), como desenhado com nanquim. Espessura varia dentro do mesmo desenho.
- **Formas:** cabeças grandes, corpos pequenos, mãos exageradas. Macabro com cara de brinquedo.
- **Sombreamento:** duas tintas por área (base e sombra chapada). Sem degradê, sem textura interna.
- **Olhos:** o recurso mais expressivo. Os chefes se distinguem principalmente por olhos e silhueta.
- **Regra de silhueta:** todo personagem e item precisa ser reconhecível **em preto sólido**. Teste isso em cada asset.

### 3.2 Paleta

Mantém a lógica do documento anterior, agora mais saturada porque o 2D perde a luz volumétrica:

| Uso | Hex |
|---|---|
| Fundo de cena | `#141019` |
| Contorno | `#0B0910` |
| Feltro do pano | `#37503F` |
| Casa vermelha | `#C0392F` |
| Casa preta | `#1C1822` |
| Casa verde | `#2E8B57` |
| Marfim (bola, fichas) | `#E8DCC0` |
| Destaque Ante 1 (sódio) | `#E8B04B` |
| Destaque Ante 2 (ferrugem) | `#C25A2A` |
| Destaque Ante 3 (carmesim) | `#B0212C` |
| Nixie / número aceso | `#FF9A2E` |

### 3.3 Tamanhos de asset (a 1080p)

| Asset | Tamanho | Nota |
|---|---|---|
| Crupiê (corpo inteiro visível) | 520×640 | PNG com alpha, partes separadas para animação |
| Chefes | 520×640 | mesma estrutura de partes |
| Roda | gerada por código | casas desenhadas por script, não pintadas |
| Ficha | 96×96 | 3 variações |
| Amuleto (ícone) | 160×160 | moldura comum + arte interna 120×120 |
| Carta da Casa / Tábua | 240×340 | formato de carta |
| Fundo por Ante | 1920×1080 | escuro, sem foco |
| Maleta, cofre, pote, placar | 320 a 640 px | objetos de mesa |

### 3.4 Animação

**Nada de frame a frame.** Use recorte com ossos (o pacote 2D Animation da Unity, ou Spine/DragonBones se preferir): o Crupiê é desenhado uma vez em partes — cabeça, tronco, dois braços, duas mãos, mandíbula, olhos — e animado por transformação. É o que Cult of the Lamb faz, e o que torna 40 animações viáveis para uma pessoa só.

Isso também resolve a consistência da arte gerada por IA: você gera **um** desenho bom do personagem e recorta, em vez de pedir à IA 12 poses coerentes, que é onde ela falha.

---

## 4. Como a ambientação sobrevive em 2D

O manual continua válido. O que muda é a execução de cada um dos cinco níveis:

| Nível | Em 3D era | Em 2D vira |
|---|---|---|
| Som | igual | **igual, e agora é ainda mais importante** |
| Luz | luzes e névoa | luzes 2D (URP), vinheta pesada, escurecimento das bordas, poeira em partículas |
| Crupiê | modelo e rig 3D | recorte animado que **encara, inclina, recua e levanta** |
| Câmera | primeira pessoa presa | zoom, shake, tilt e desfoque do fundo nos momentos de tensão |
| Cenário | três salas modeladas | **três fundos ilustrados** |

E o essencial não muda: o segundo de silêncio depois que a bola para, a batida de dedo aos 20 segundos, o rodo puxando as fichas devagar, a descida entre Antes (agora uma transição ilustrada com paralaxe, não um corredor 3D).

**Os estágios de luz em 2D:**

| Estágio | O que acontece na tela |
|---|---|
| 0 | Vinheta fechada, fundo quase invisível, só a mesa iluminada |
| 1 | Flash na luz, fichas saltam, o Crupiê recua |
| 2 | A vinheta abre: dá para ver o fundo do cômodo pela primeira vez |
| 3 | A roda brilha, fichas chovendo em partículas, o Crupiê levanta |
| 4 | Fundo todo iluminado e saturado, a cadeira do Crupiê vazia, 1 s de silêncio |

---

## 5. Pipeline de arte com IA

### 5.1 O problema real: consistência

Gerar uma imagem bonita é fácil; gerar 40 no mesmo estilo não é. O método que funciona:

1. **Feche a folha de estilo primeiro.** Gere de 20 a 30 variações só do Crupiê até achar UMA que seja o estilo do jogo. Essa imagem vira sua referência-mestra.
2. **Todo asset seguinte é gerado com essa imagem como referência** (image-to-image ou reference image), nunca só por texto.
3. **Bloco de estilo fixo** em todos os prompts, igual ao que fizemos antes:
   ```
   hand-drawn 2D game asset, thick uneven black ink outline, flat two-tone shading,
   no gradients, cute-macabre cartoon, oversized head and hands, expressive eyes,
   dark muted palette, isolated on plain background, full body, front view, no text
   ```
4. **Limpeza obrigatória no Krita ou Photoshop:** recortar o alpha, reforçar o contorno, reduzir para a paleta indexada da seção 3.2. É esse passo que faz 40 assets de origens diferentes parecerem o mesmo jogo.
5. **Separe em partes** logo depois da limpeza, para o rig 2D (cabeça, tronco, braços, mãos, olhos, mandíbula).

### 5.2 Ordem de geração

1. Crupiê padrão (a referência-mestra)
2. Os 6 chefes, com o Crupiê como referência
3. Moldura de amuleto e 5 ícones, para fechar o padrão dos ícones
4. Os 3 fundos
5. Resto dos ícones e objetos

### 5.3 Declaração na Steam

A Steam exige que o desenvolvedor **declare o uso de conteúdo gerado por IA** no formulário de submissão do jogo, dizendo onde ela foi usada. Isso não impede a publicação, mas omitir gera problema na revisão. Registre desde já, num arquivo `Docs/AI_Disclosure.md`, quais assets foram gerados e como foram tratados — na hora de publicar você só copia. Confirme o texto exato da política no Steamworks quando for submeter, porque ela muda.

---

## 6. Lista de assets 2D da demo

**Personagens (7)**

- [ ] Crupiê padrão, em partes
- [ ] 6 chefes, em partes, reusando o corpo do Crupiê quando possível

**Mesa (12)**

- [ ] Pano de apostas (imagem base + estados de destaque)
- [ ] Pista de vizinhos
- [ ] 3 fichas · pilha de fichas · maço de notas
- [ ] Pote de marfim (giros) · marcas de giz (empurrões)
- [ ] Maleta aberta e fechada · cofre com fechadura · placar de palhetas

**Itens (60+)**

- [ ] Moldura de amuleto (comum, incomum, raro, lendário) + 40 ícones
- [ ] Carta da Casa: verso + 12 frentes
- [ ] Tábua: 8 ícones
- [ ] 3 pacotes

**Cenário (6)**

- [ ] 3 fundos de Ante + 3 camadas de paralaxe

**Interface (as maiores economias)**

- [ ] Fonte (uma display com personalidade + uma legível para números)
- [ ] Botões, caixas, tooltips — tudo em 9-slice, para não desenhar cada tamanho

---

## 7. Roadmap revisado

| Marco | Entrega | Duração |
|---|---|---|
| **M5a — Migração técnica** | Camada de apresentação 2D, novo layout, roda 2D, com placeholders coloridos | 1–2 semanas |
| **M5b — Folha de estilo** | Crupiê definitivo + 3 ícones + 1 fundo. **Gate: o print mudo** | 1 semana |
| **M5c — Produção de assets** | Chefes, ícones, cartas, fundos | 2–3 semanas |
| **M5d — Atmosfera** | Áudio, luzes 2D, estágios, reações do Crupiê, transições | 2 semanas |
| M6 | UI final, tutorial, opções | 2 semanas |
| M7 | Steam | 2 semanas |

Economia estimada em relação ao plano 3D: algo entre 4 e 6 semanas, e o risco de travar por falta de habilidade técnica cai muito.

---

## 8. Prompts do Claude Code

### Prompt 2D-A — Migração da camada de apresentação

```
Leia Docs/PLENO_Migracao_2D.md inteiro antes de começar. Vamos migrar o PLENO de 3D para
2D. Pleno.Core NÃO pode ser tocado: se algum teste dos 267 quebrar, a migração está errada.

1. Faça um inventário do que existe em Pleno.Presentation hoje e me mostre, antes de apagar
   nada, a lista do que vai ser removido, adaptado e criado. Espere minha confirmação.

2. Remova o pipeline retrô 3D (RetroRenderSettings, PlenoRetroPost, PlenoPS1, o
   RetroResolutionController) e a geração de malha 3D da roda. Mantenha WheelLayout e toda a
   matemática de ângulos e do solver da bola: eles são reaproveitados.

3. Converta o projeto para 2D: URP 2D Renderer, câmera ortográfica, resolução de referência
   1920x1080 com Canvas Scaler em Scale With Screen Size.

4. Monte o novo layout da seção 2 com placeholders coloridos: um retângulo para o Crupiê no
   topo, a roda ao centro, o pano embaixo, as barras de amuleto e consumível, os contadores
   de Pontos e Mult, o botão Girar. Tudo ligado aos mesmos eventos do Core que a versão 3D
   já usava.

5. Pós-processamento 2D simples: vinheta, grão sutil e leve aberração, com liga/desliga nas
   opções. Nada de baixa resolução: a arte é 1080p.

6. Um GraphicsProfile (ScriptableObject) com a paleta da seção 3.2, para nenhum código ter
   cor escrita na mão.

Verificação: rode uma mesa inteira do começo ao fim no novo layout, com os 267 testes
passando, e me mostre um print. O jogo tem de continuar 100% jogável, mesmo feio.
```

### Prompt 2D-B — Roda 2D procedural

```
Leia Docs/PLENO_Migracao_2D.md (seção 2). Implemente a roda 2D.

1. WheelRenderer2D: desenha a roda como uma ELIPSE (560x360) com N setores, gerada por
   código a partir da lista de casas do Core, suportando de 18 a 48 casas. Use uma malha 2D
   com vertex color (não sprites prontos), para a roda continuar mudando quando o jogador
   clona ou apaga casas.

2. Números e ícones das casas desenhados por cima, acompanhando a rotação, sempre legíveis:
   o número gira junto com a roda mas nunca fica de cabeça para baixo.

3. BallRenderer2D: a bola percorre a elipse. Reaproveite o solver já testado da fase 5.2 —
   a lógica de ângulos não muda, muda só a projeção para a elipse (x = a·cosθ, y = b·sinθ).
   Mantenha os testes do solver passando.

4. Aprimoramentos de casa em 2D: dourada brilhando, vidro translúcido com rachadura, aço
   metálico, magnética com ícone. Todos desenhados por código ou por sprite de overlay.

5. Empurrão: a bola pula para a casa vizinha com um arco curto e um "tec".

Verificação: rode 10 giros e confirme que a bola para na casa que o log registrou; clone um
número e confirme que a roda se refaz; teste com 18 e com 48 casas.
```

### Prompt 2D-C — Crupiê 2D reativo

```
Leia Docs/PLENO_Migracao_2D.md (seções 3.4 e 4) e Docs/PLENO_Ambientacao_M5-4.md (seção 5).
Implemente o Crupiê 2D com rig de recorte.

1. Instale o pacote 2D Animation da Unity. Monte um prefab DealerRig2D com as partes
   separadas (cabeça, tronco, braço esquerdo, braço direito, mão esquerda, mão direita,
   olhos, mandíbula), usando retângulos coloridos como placeholder até a arte chegar.

2. Porte a máquina de estados DealerPresence do documento de ambientação para 2D:
   Idle, Watching, Impatient, Serving, Collecting, Recoil, Standing, Gone.
   - Watching: a cabeça e os olhos acompanham o cursor do jogador, com atraso de 0,4 s
   - Impatient: batida de dedo aos 20 s e duas aos 45 s
   - Recoil e Standing disparam pelos estágios de luz 1/2 e 3

3. DealerVariant2D (ScriptableObject) por chefe: sprites das partes, deslocamento de
   silhueta, objeto que ele coloca na mesa, tique e efeito de ambiente. Crie os 6 como
   assets com placeholders.

4. Estágios de luz 2D (seção 4): vinheta, luzes 2D, saturação do fundo e partículas por
   estágio, persistindo até o fim da mesa.

5. Trocar os sprites das partes não pode exigir mudar código: a arte final entra só
   substituindo os sprites do prefab.

Verificação: mostre um GIF do Watching e do Impatient, e prints dos quatro estágios de luz.
```

---

## 9. O que fazer agora

1. Rode o **prompt 2D-A** e confirme que o jogo continua jogável com placeholders.
2. Feche a **folha de estilo** do Crupiê (seção 5.1) antes de gerar qualquer outro asset.
3. Refaça o **teste do print mudo**: mostre a tela sem interface e pergunte que lugar é aquele.

Enquanto a arte não chega, o jogo inteiro roda com retângulos coloridos — e é assim que deve ser, porque o gate do M3 ainda é sobre diversão, não sobre beleza.
