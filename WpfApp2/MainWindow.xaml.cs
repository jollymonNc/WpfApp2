using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Resources;
using System.IO;

/*
 * 
 * Need to ADD:  
 *  
 * Make sure dealer handling of aces (and soft 17 for example) is correct
 * Splitting
 * Bug - splitting sometimes (rarely) shows enabled, when it shouldn't
 * Bug - splitting is not grayed out immediately after 1st split
 * Surrender
 * Strategy check and percentage
 * Extra players/ simmulation
 * House rules adjustment
 * Simulation 
 * Better random (needed for rapid simulation, where time random is a problem)
 * Time (per hannd, for simulation)
 * Results save and graphing
 * Graphics
 * 
 * Splitting.  If cards are equal, allow split button.  If split, add new hand, and add the split card to the new hand.  Remove the split card from the old hand.  Deal a new card to old hand, and proceed as normal.  Once hand is done, proceed to next hand and do the same.  Once all hands are done, calculate the total winnings.
 * 
 */

namespace WpfApp2
{
    public enum Suit
    {
        clubs,
        diamonds,
        spades,
        hearts
    }

    public enum CardRank
    {
        Ace = 1,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King
    }



    public enum PlayResult
    {
        push,
        winner,
        loser,
        surrender,
        blackjack
    }

    public enum PlayType
    {
        hit,
        stand,
        dbl,
        split,
        surrender
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Blackjack myBlackjack;
   
        public MainWindow()
        {
            InitializeComponent();

            myBlackjack = new Blackjack(6);


        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            myBlackjack = new Blackjack(6);
        }

        private void HitButton_Click(object sender, RoutedEventArgs e)
        {
            myBlackjack.hit();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Stand button
            myBlackjack.Stand();
        }

        private void DealButton_Click(object sender, RoutedEventArgs e)
        {
            myBlackjack.Deal();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            /*try
            {
                myBlackjack.ChangeBet(decimal.Parse(CurrentBet.Text));
            }
            catch
            {

            }*/
        }

        private void DoubleButton_Click(object sender, RoutedEventArgs e)
        {
            myBlackjack.Double();
        }

        private void SplitButton_Click(object sender, RoutedEventArgs e)
        {
            myBlackjack.Split();
        }

        private void SurrenderButton_Click_1(object sender, RoutedEventArgs e)
        {
            myBlackjack.Surrender();
        }
    }

    public class Blackjack
    {
        private CardDeck myCardDeck;
        private Hand dealerHand;
        private List<Player> players;
        private int numDecks;
        public int handsPlayed;
        public int handsCorrect;
        Image[,] cardImageSlot = new Image[2,5];

        MainWindow Form = Application.Current.Windows[0] as MainWindow;
        

        public Blackjack(int numDecks)
        {
            handsPlayed = 0;
            handsCorrect = 0;;


            cardImageSlot[0,0] = Form.CardImage_S11;
            cardImageSlot[0,1] = Form.CardImage_S12;
            cardImageSlot[0,2] = Form.CardImage_S13;
            cardImageSlot[0,3] = Form.CardImage_S14;
            cardImageSlot[0,4] = Form.CardImage_S15;
            cardImageSlot[1,0] = Form.CardImage_S31;
            cardImageSlot[1,1] = Form.CardImage_S32;
            cardImageSlot[1,2] = Form.CardImage_S33;
            cardImageSlot[1,3] = Form.CardImage_S34;
            cardImageSlot[1,4] = Form.CardImage_S35;

            players = new List<Player>();

            players.Add(new Player(cardImageSlot));

            Form.CurrentBet.Text = "10.00";

            this.numDecks = numDecks;

            myCardDeck = new CardDeck(numDecks);

            // Burn the first card, per blackjack rules
            myCardDeck.GetNextCard();

            Deal();
        }

        public void Deal()
        {
            Image[] currentSlot = new Image[5];

            enableBetButtons(true);
            Form.ResultText.Text = "";
            Form.ResultText.Background = Brushes.White;

            if (myCardDeck.EndOfDeck())
            {
                // Current deck is past the cut card, so Create a new deck
                myCardDeck = new CardDeck(this.numDecks);

                // Burn the first card, per blackjack rules
                myCardDeck.GetNextCard();
            }

            Display.Clear();  // Erase any old cards on the display

            for (int i = 0; i < 5; i++)
                currentSlot[i] = cardImageSlot[0, i];

            dealerHand = new Hand(currentSlot);
            players[0].newDeal();

            // Deal cards
            players[0].AddCard(myCardDeck.GetNextCard());
            dealerHand.Add(myCardDeck.GetNextCard());

            players[0].AddCard(myCardDeck.GetNextCard());
            dealerHand.Add(myCardDeck.GetNextCard());

            Form.DealerHand.Text = dealerHand.getDealerHiddenText();
            Display.DisplayHand(0, dealerHand, 0, true);

            if (CheckForQuickWinner() == true)
            {
                enableBetButtons(false);
                Display.DisplayHand(0, dealerHand, 0, false);
            }
        }



        public void finishHands()
        {
            // All hands have been played out

            while (dealerHand.value < 17)
                dealerHand.Add(myCardDeck.GetNextCard());

            Form.DealerHand.Text = dealerHand.getDisplayText();
            Display.DisplayHand(0, dealerHand, 0, false);

            // process whether we won or lost
            players[0].TrueUp(dealerHand.value);


        }

        public Boolean CheckForQuickWinner()
        {
            if ((dealerHand.value == 21) && (players[0].getCurrentHandValue() == 21))
            {
                players[0].ProcessGameResult(PlayResult.push, players[0].getCurrentBetValue());
                return true;
            }
            else if (dealerHand.value == 21)
            {
                players[0].ProcessGameResult(PlayResult.loser, players[0].getCurrentBetValue());
                return true;
            }
            else if (players[0].getCurrentHandValue() == 21)
            {
                players[0].ProcessGameResult(PlayResult.blackjack, players[0].getCurrentBetValue());
                return true;
            }
            
            return false;
        }

        public void enableBetButtons (Boolean enable)
        {
            Form.StandButton.IsEnabled = enable;
            Form.HitButton.IsEnabled = enable;
            Form.DoubleButton.IsEnabled = enable;
            Form.SurrenderButton.IsEnabled = enable;
            Form.SplitButton.IsEnabled = false;

            Form.DealButton.IsEnabled = !enable;
        }

        public void hit()
        {
            Boolean bust;

            EvaluatePlay(PlayType.hit, players[0].myHands[players[0].currentHand]);

            bust = players[0].AddCard(myCardDeck.GetNextCard());

            Form.DoubleButton.IsEnabled = false;
            Form.SurrenderButton.IsEnabled = false;  // Can't double or surrender after hitting

            if (bust == true)
            {
                enableBetButtons(false);
            }
        }

        public void Double ()
        {
            EvaluatePlay(PlayType.dbl, players[0].myHands[players[0].currentHand]);

            players[0].Double(myCardDeck.GetNextCard());

            GetNextHand();  // Once you've doubled and you got your card, you're done with this hand
        }

        public void Stand ()
        {
            EvaluatePlay(PlayType.stand, players[0].myHands[players[0].currentHand]);

            GetNextHand();
        }

        public void Split()
        {
            EvaluatePlay(PlayType.split, players[0].myHands[players[0].currentHand]);

            players[0].Split(myCardDeck.GetNextCard());
        }

        public void Surrender()
        {
            EvaluatePlay(PlayType.surrender, players[0].myHands[players[0].currentHand]);

            players[0].myHands[players[0].currentHand].surrender = true;

            GetNextHand();
        }

        public void GetNextHand()
        {
            if (players[0].PlayNextHand() == false)
            {
                // no more hands to display
                enableBetButtons(false);
               
                finishHands();
            }
            else
            {
                enableBetButtons(true);

                // show the next hand and add a card (since it was created off a split, we need a new card
                players[0].AddCard(myCardDeck.GetNextCard());
            }
        }

        public PlayType EvaluatePlay(PlayType play, Hand myHand)
        {
            int row = 0;
            int column = 0;
            PlayType correctPlay;

            PlayType[,] truthTable = new PlayType[28, 10]
            {
                {PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // 8
                {PlayType.hit,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // 9
                {PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.hit,PlayType.hit}, // 10
                {PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.hit}, // 11
                {PlayType.hit,PlayType.hit,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // 12
                {PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // 13
                {PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // 14
                {PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.surrender,PlayType.hit}, // 15
                {PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.hit,PlayType.hit,PlayType.surrender,PlayType.surrender,PlayType.surrender}, // 16
                {PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand}, // 17
                {PlayType.hit,PlayType.hit,PlayType.hit,PlayType.dbl,PlayType.dbl,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // a 2
                {PlayType.hit,PlayType.hit,PlayType.hit,PlayType.dbl,PlayType.dbl,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // a 3
                {PlayType.hit,PlayType.hit,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // a 4
                {PlayType.hit,PlayType.hit,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // a 5
                {PlayType.hit,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // a 6
                {PlayType.stand,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.stand,PlayType.stand,PlayType.hit,PlayType.hit,PlayType.hit}, // a 7
                {PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand}, // a 8
                {PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand}, // a 9
                {PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.hit,PlayType.hit,PlayType.hit}, // 2 2
                {PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.hit,PlayType.hit,PlayType.hit}, // 3 3
                {PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.split,PlayType.split,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // 4 4
                {PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.dbl,PlayType.hit,PlayType.hit}, // 5 5
                {PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // 6 6
                {PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.hit,PlayType.hit,PlayType.hit,PlayType.hit}, // 7 7
                {PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split}, // 8 8
                {PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.stand,PlayType.split,PlayType.split,PlayType.stand,PlayType.stand}, // 9 9
                {PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand,PlayType.stand}, // 10 10
                {PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split,PlayType.split} // a a
            };

            if (myHand.handCards.Count == 2)
            {
                if (myHand.handCards[0].FaceValue == myHand.handCards[1].FaceValue)
                {
                    row = myHand.handCards[0].FaceValue + 16;  // i.e. a 2 and 2 translates to row 18 in the table above.  an ace ace translates to row 27.
                }
                else if (myHand.handCards[0].rank == CardRank.Ace || myHand.handCards[1].rank == CardRank.Ace)
                {
                    row = myHand.value - 10 + 7;  // treat aces as ones (subtract 10 from value), and then start at row 10 (a2, i.e. 3 value, starts at row 10)
                }
                else
                {
                    if (myHand.value <= 8)
                        row = 0;
                    else if (myHand.value >= 17)
                        row = 9;
                    else
                        row = myHand.value - 8;
                }
            }
            else  // More than 2 cards already
            {
                if (myHand.value <= 8)
                    row = 0;
                else if (myHand.value >= 17)
                    row = 9;
                else
                    row = myHand.value - 8;
            }

            if (dealerHand.handCards[0].FaceValue == 1)
                column = 9;  // Treat aces as 11's
            else
                column = dealerHand.handCards[0].FaceValue - 2;

            correctPlay = truthTable[row, column];

            if (myHand.handCards.Count > 2 && (correctPlay == PlayType.dbl || correctPlay == PlayType.surrender))
                correctPlay = PlayType.hit;  // Can't double or surrender when more than 2 cards are showing, so the correct play is a hit instead

            Form.Strategy_Eval.Foreground = Brushes.White;

            handsPlayed += 1;

            if (play == correctPlay)
            {
                handsCorrect += 1;
                Form.Strategy_Eval.Background = Brushes.Green;
                Form.Strategy_Eval.Text = "CORRECT!!";
            }
            else
            {
                Form.Strategy_Eval.Background = Brushes.Red;

                if (correctPlay == PlayType.dbl)
                    Form.Strategy_Eval.Text = "INCORRECT, SHOULD DOUBLE!";
                else if (correctPlay == PlayType.hit)
                    Form.Strategy_Eval.Text = "INCORRECT, SHOULD HIT!";
                else if (correctPlay == PlayType.split)
                    Form.Strategy_Eval.Text = "INCORRECT, SHOULD SPLIT!";
                else if (correctPlay == PlayType.stand)
                    Form.Strategy_Eval.Text = "INCORRECT, SHOULD STAND!";
                else if (correctPlay == PlayType.surrender)
                    Form.Strategy_Eval.Text = "INCORRECT, SHOULD SURRENDER!";
            }

            float percentageRight = ((float)handsCorrect / (float)handsPlayed) * (float) 100.0;

            Form.NumberRight.Text = handsCorrect + "/" + handsPlayed;
            Form.PercentageRight.Text = percentageRight + "%";


            return correctPlay;
        }
    }

    public class Player
    {
        private Money myMoney;
        public List<Hand> myHands;
        public int currentHand;

        Image [,]cardImageSlot;

        MainWindow Form = Application.Current.Windows[0] as MainWindow;

        public Player (Image [,] cardImageSlot)
        {
            currentHand = 0;
            this.cardImageSlot = cardImageSlot;
            myHands = new List<Hand>();
            myMoney = new Money(200.00m);
        }

        public void newDeal ()
        {
            myHands = new List<Hand>();
            currentHand = 0;

            AddHand();
        }

        public void AddHand ()
        {
            Image [] currentSlot = new Image [5];

            for (int i = 0; i < 5; i++)
                currentSlot[i] = cardImageSlot[currentHand, i];
            myHands.Add(new Hand(currentSlot));
        }

        public Boolean AddCard (Card newCard)
        {
            myHands[currentHand].Add(newCard);
            Form.Hand1.Text = myHands[currentHand].getDisplayText();
            Display.DisplayHand(1, myHands[currentHand], currentHand, false);

            if (myHands[currentHand].value > 21)
            {
                Form.ResultText.Text = "BUST";
                Form.ResultText.Background = Brushes.Red;
                myMoney.UpdatePot(0 - myHands[currentHand].bet);


                return true;
            }
            else
                return false;

        }

        public void ProcessGameResult(PlayResult result, decimal bet)
        {
            if (result == PlayResult.push)
            {
                Form.ResultText.Text = "PUSH";
                Form.ResultText.Background = Brushes.Yellow;
                myMoney.UpdatePot(0);
            }
            else if (result == PlayResult.winner)
            {
                Form.ResultText.Text = "WINNER!";
                Form.ResultText.Background = Brushes.Green;
                myMoney.UpdatePot(bet);
            }
            else if (result == PlayResult.loser)
            {
                Form.ResultText.Text = "LOSER";
                Form.ResultText.Background = Brushes.Red;
                Form.ResultText.Foreground = Brushes.White;
                myMoney.UpdatePot(0 - bet);
            }
            else if (result == PlayResult.surrender)
            {
                Form.ResultText.Text = "SURRENDER";
                Form.ResultText.Background = Brushes.Orange;
                myMoney.UpdatePot(0 - (bet/2));  // Lose only half your bet on surrenders
            }
            else if (result == PlayResult.blackjack)
            {
                Form.ResultText.Text = "BLACKJACK";
                Form.ResultText.Background = Brushes.Green;
                myMoney.UpdatePot(bet * 3 / 2);
            }

        }

        public int getCurrentHandValue()
        {
            return myHands[currentHand].value;
        }

        public decimal getCurrentBetValue()
        {
            return myHands[currentHand].bet;
        }

        public void Double(Card newCard)
        {
            myHands[currentHand].bet *= 2;

            // Get your one additional card, and finish the hand
            myHands[currentHand].Add(newCard);

            Form.Hand1.Text = myHands[currentHand].getDisplayText();
            Display.DisplayHand(1, myHands[currentHand], currentHand, false);
        }

        public void Split (Card newCard)
        {
            Card splitCard;
            Image[] currentSlot = new Image[5];

            for (int i = 0; i < 5; i++)
                currentSlot[i] = cardImageSlot[1, i];

            splitCard = myHands[currentHand].handCards[1];
            myHands[currentHand].Remove(1);

            myHands[currentHand].Add(newCard); // Add the new card to the first hand

            // Creat a new hand and add the split off card to it
            myHands.Add(new Hand(currentSlot));
            myHands[myHands.Count-1].Add(splitCard);

            Form.Hand1.Text = myHands[currentHand].getDisplayText();
            Display.DisplayHand(1, myHands[currentHand], currentHand, false);
            Display.DisplayHand(1, myHands[myHands.Count - 1], myHands.Count - 1, false);
        }
        public Boolean PlayNextHand()
        {
            if (myHands.Count == currentHand + 1)
                return false;  // no more hands to play
            else
            {
                currentHand++;

                Form.Hand1.Text = myHands[currentHand].getDisplayText();
                Display.DisplayHand(1, myHands[currentHand], currentHand, false);

                return true;
            }
        }

        public void TrueUp (int dealerValue)
        {
            // see whether we won or lost each hand

            for (int i = 0; i < myHands.Count; i++)
            {
                if (myHands[i].surrender == true)
                {
                    ProcessGameResult(PlayResult.surrender, myHands[i].bet);
                }
                else if (myHands[i].value > 21)
                {
                    // ignore this hand - we already deducted when we busted
                }
                else if (myHands[i].value > dealerValue || dealerValue > 21)
                {
                    ProcessGameResult(PlayResult.winner, myHands[i].bet);
                }
                else if (getCurrentHandValue() == dealerValue)
                {
                    ProcessGameResult(PlayResult.push, myHands[i].bet);
                }
                else
                {
                    ProcessGameResult(PlayResult.loser, myHands[i].bet);
                }
            }
        }
    }


    public class Money
    {
        public decimal pot;
        MainWindow Form = Application.Current.Windows[0] as MainWindow;

        public Money (decimal Pot)
        {
           
            this.pot = Pot;
            DisplayMoney();
        }

        public void DisplayMoney ()
        {
            Form.CurrentPot.Text = String.Format("{0:C}", pot);
        }

        public void UpdatePot(decimal adjust)
        {
            pot += adjust;
            DisplayMoney();
        }
    }



    public class Hand
    {
        public List<Card> handCards;
        public int value;  // Blackjack count (0-21)
        public decimal bet;
        public Boolean surrender;
        Image [] cardImageSlot = new Image [5];

        MainWindow Form = Application.Current.Windows[0] as MainWindow;

        public Hand (Image [] myCardImageSlot)
        {
            handCards = new List<Card>();

            for (int i = 0; i < 5; i++)
                cardImageSlot[i] = myCardImageSlot[i];

            //cardImageSlot = myCardImageSlot;
            /*cardImageSlot[0] = Form.CardImage_S11;
            cardImageSlot[1] = Form.CardImage_S12;
            cardImageSlot[2] = Form.CardImage_S13;
            cardImageSlot[3] = Form.CardImage_S14;
            cardImageSlot[4] = Form.CardImage_S15;*/

            // cardImageSlot[2].Source = new BitmapImage(new Uri("Resources/6_of_hearts.png", UriKind.Relative));

            value = 0;
            surrender = false;
            try
            {
                
                bet = decimal.Parse(Form.CurrentBet.Text);
            }
            catch
            {
                bet = 10.00m;
                Form.CurrentBet.Text = "10";
            }
        }

        public void Add (Card newCard)
        {
            Boolean ace = false;
            handCards.Add(newCard);

            this.value = 0;

            for (int i = 0; i < handCards.Count; i++)
            {
                this.value += (int) handCards[i].FaceValue;
                if (handCards[i].rank == CardRank.Ace)
                    ace = true;
            }

            if (ace == true && this.value <= 11)
                this.value += 10;  // Convert the ace from a one to an eleven

            // Check for Split possibility
            if ((handCards.Count == 2) && (handCards[0].FaceValue == handCards[1].FaceValue))
                Form.SplitButton.IsEnabled = true;
            //else
            //    Form.SplitButton.IsEnabled = false;
        }


        public void Remove (int index)
        {
            this.value -= (int)handCards[index].FaceValue;
            handCards.RemoveAt(index);
        }

        public string getDisplayText ()
        {
            StringBuilder myText = new StringBuilder();

            for (int i=0; i < handCards.Count; i++)
            {
                myText.Append(handCards[i].rank);
                myText.Append(" ");
            }
            myText.Append(value);

            // Show the cards
            /*
            for (int i = 0; i < 5; i++)
                cardImageSlot[i].Source = null;

            for (int i = 0; i < handCards.Count; i++)
                //cardImageSlot[i].Source = handCards[i].cardImage.Source;
                Display.DisplayHand(0, 0, handCards[i].cardImage);*/
            //Display.DisplayHand(0, this);

            return myText.ToString();
        }

        public string getDealerHiddenText()
        {
            StringBuilder myText = new StringBuilder();

            myText.Append(handCards[0].rank);
            myText.Append(" XXX");

            return myText.ToString();
        }
    }



    public class Card
    {
        public Suit suit;
        public CardRank rank;
        public int FaceValue;
        public Image cardImage;

        public string[,] cardImageSources = new string[4, 13] 
        {
            {"Resources/ace_of_clubs.png","Resources/2_of_clubs.png","Resources/3_of_clubs.png","Resources/4_of_clubs.png","Resources/5_of_clubs.png","Resources/6_of_clubs.png","Resources/7_of_clubs.png","Resources/8_of_clubs.png","Resources/9_of_clubs.png","Resources/10_of_clubs.png","Resources/jack_of_clubs.png","Resources/queen_of_clubs.png","Resources/king_of_clubs.png"},  // clubs
            {"Resources/ace_of_diamonds.png","Resources/2_of_diamonds.png","Resources/3_of_diamonds.png","Resources/4_of_diamonds.png","Resources/5_of_diamonds.png","Resources/6_of_diamonds.png","Resources/7_of_diamonds.png","Resources/8_of_diamonds.png","Resources/9_of_diamonds.png","Resources/10_of_diamonds.png","Resources/jack_of_diamonds.png","Resources/queen_of_diamonds.png","Resources/king_of_diamonds.png"},  // diamonds
            {"Resources/ace_of_spades.png","Resources/2_of_spades.png","Resources/3_of_spades.png","Resources/4_of_spades.png","Resources/5_of_spades.png","Resources/6_of_spades.png","Resources/7_of_spades.png","Resources/8_of_spades.png","Resources/9_of_spades.png","Resources/10_of_spades.png","Resources/jack_of_spades.png","Resources/queen_of_spades.png","Resources/king_of_spades.png"},  // spades
            {"Resources/ace_of_hearts.png","Resources/2_of_hearts.png","Resources/3_of_hearts.png","Resources/4_of_hearts.png","Resources/5_of_hearts.png","Resources/6_of_hearts.png","Resources/7_of_hearts.png","Resources/8_of_hearts.png","Resources/9_of_hearts.png","Resources/10_of_hearts.png","Resources/jack_of_hearts.png","Resources/queen_of_hearts.png","Resources/king_of_hearts.png"}  // hearts
        };



        // Constructor
        public Card (CardRank rank, Suit suit)
        {
            this.suit = suit;
            this.rank = rank;

            cardImage = new Image();

            this.cardImage.Source = new BitmapImage(new Uri(cardImageSources[(int)suit,(int)rank-1], UriKind.Relative));

            this.FaceValue = (int) rank;
            if (FaceValue > 10)
                FaceValue = 10;  // Face cards are all worth 10
        }
    }

    public class CardDeck
    {
        private int currentCard;
        private List<Card> cards;
        private int cutCard;

        // Constructor
        public CardDeck(int numDecks)
        {
            MainWindow Form = Application.Current.Windows[0] as MainWindow;

            this.currentCard = 0;

            cards = new List<Card>();
            for (int i = 0; i < numDecks; i++)
            {
                for (CardRank newCard = CardRank.Ace; newCard <= CardRank.King; newCard++)
                {
                    for (Suit newSuit = Suit.clubs; newSuit <= Suit.hearts; newSuit++)
                    {
                        Card myCard = new Card(newCard, newSuit);
                        cards.Add(myCard);

                    }

                }
            }
            cards.Shuffle();

            cutCard = (int) (numDecks * 52 * .75);

            PrintAll();
        }

        public void PrintAll()
        {
            MainWindow Form = Application.Current.Windows[0] as MainWindow;
            StringBuilder mystring = new StringBuilder();

            Form.CardListBox.Items.Clear();

            for (int i = 0; i < this.cards.Count; i++)
            {
                mystring.Append(cards[i].rank);
                mystring.Append(" ");
                mystring.Append(cards[i].suit);
                Form.CardListBox.Items.Add(mystring.ToString());
                mystring.Clear();
            }
        }

        public void Shuffle ()
        {
            MainWindow Form = Application.Current.Windows[0] as MainWindow;

            cards.Shuffle();

            PrintAll();
        }

        public Card GetNextCard ()
        {
            return cards[currentCard++];
        }

        public Boolean EndOfDeck()
        {
            if (currentCard > cutCard)
                return true;
            else
                return false;
        }

    }

    public static class Display
    {
        private static Image[,,] cardImageSlot = new Image[2, 2, 5];  // Each player (2, inclusive of dealer) can have 2 hands, up to 5 cards in each hand

        static Display()
        {
            MainWindow Form = Application.Current.Windows[0] as MainWindow;


            cardImageSlot[0, 0, 0] = Form.CardImage_S21;
            cardImageSlot[0, 0, 1] = Form.CardImage_S22;
            cardImageSlot[0, 0, 2] = Form.CardImage_S23;
            cardImageSlot[0, 0, 3] = Form.CardImage_S24;
            cardImageSlot[0, 0, 4] = Form.CardImage_S25;
            cardImageSlot[0, 1, 0] = Form.CardImage_S21;
            cardImageSlot[0, 1, 1] = Form.CardImage_S22;
            cardImageSlot[0, 1, 2] = Form.CardImage_S23;
            cardImageSlot[0, 1, 3] = Form.CardImage_S24;
            cardImageSlot[0, 1, 4] = Form.CardImage_S25;

            cardImageSlot[1, 0, 0] = Form.CardImage_S11;
            cardImageSlot[1, 0, 1] = Form.CardImage_S12;
            cardImageSlot[1, 0, 2] = Form.CardImage_S13;
            cardImageSlot[1, 0, 3] = Form.CardImage_S14;
            cardImageSlot[1, 0, 4] = Form.CardImage_S15;
            cardImageSlot[1, 1, 0] = Form.CardImage_S31;
            cardImageSlot[1, 1, 1] = Form.CardImage_S32;
            cardImageSlot[1, 1, 2] = Form.CardImage_S33;
            cardImageSlot[1, 1, 3] = Form.CardImage_S34;
            cardImageSlot[1, 1, 4] = Form.CardImage_S35;
        }

        public static void DisplayHand(int playerNumber, Hand myHand, int currentHand, Boolean dealerHidden)
        {
            MainWindow Form = Application.Current.Windows[0] as MainWindow;

            for (int i = 0; i < 5; i++)
                cardImageSlot[playerNumber, currentHand, i].Source = null;

            if (dealerHidden == true)
            {
                // Only show the first card
                cardImageSlot[playerNumber, currentHand, 0].Source = myHand.handCards[0].cardImage.Source;
            }
            else
            {
                for (int i = 0; i < myHand.handCards.Count; i++)
                    cardImageSlot[playerNumber, currentHand, i].Source = myHand.handCards[i].cardImage.Source;
            }
        }

        public static void Clear ()
        {
            MainWindow Form = Application.Current.Windows[0] as MainWindow;

            Form.Strategy_Eval.Text = "";
            Form.Strategy_Eval.Background = Brushes.White;

            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    for (int k = 0; k < 5; k++)
                        cardImageSlot[i, j, k].Source = null;
        }

    }

    public static class IListExtensions
    {
        /// <summary>
        /// Shuffles the element order of the specified list.
        /// </summary>
        public static void Shuffle<T>(this IList<T> ts)
        {
            Random rnd1 = new Random();

            var count = ts.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = rnd1.Next(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }
    }

}
