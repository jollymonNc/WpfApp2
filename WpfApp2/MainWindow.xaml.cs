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
        blackjack
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Blackjack myBlackjack;

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
    }

    public class Blackjack
    {
        private CardDeck myCardDeck;
        private Hand dealerHand;
        private List<Player> players;
        private int numDecks;
  
        MainWindow Form = Application.Current.Windows[0] as MainWindow;

        public Blackjack(int numDecks)
        {
            players = new List<Player>();

            players.Add(new Player());

            Form.CurrentBet.Text = "10.00";

            this.numDecks = numDecks;

            myCardDeck = new CardDeck(numDecks);

            // Burn the first card, per blackjack rules
            myCardDeck.GetNextCard();

            Deal();
        }

        public void Deal()
        {
            enableBetButtons(true);
            Form.ResultText.Text = "";

            if (myCardDeck.EndOfDeck())
            {
                // Current deck is past the cut card, so Create a new deck
                myCardDeck = new CardDeck(this.numDecks);

                // Burn the first card, per blackjack rules
                myCardDeck.GetNextCard();
            }

            dealerHand = new Hand();
            players[0].newDeal();

            // Deal cards
            players[0].AddCard(myCardDeck.GetNextCard());
            dealerHand.Add(myCardDeck.GetNextCard());

            players[0].AddCard(myCardDeck.GetNextCard());
            dealerHand.Add(myCardDeck.GetNextCard());

            Form.DealerHand.Text = dealerHand.getDealerHiddenText();

            if (CheckForQuickWinner() == true)
                enableBetButtons(false);
        }

        public void hit()
        {
            Boolean bust;
            bust = players[0].AddCard(myCardDeck.GetNextCard());

            if (bust == true)
            {
                enableBetButtons(false);
            }
        }

        public void finishHands()
        {
            // All hands have been played out

            while (dealerHand.value < 17)
                dealerHand.Add(myCardDeck.GetNextCard());

            Form.DealerHand.Text = dealerHand.getDisplayText();

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
            Form.SplitButton.IsEnabled = false;

            Form.DealButton.IsEnabled = !enable;
        }



        public void Double ()
        {
            players[0].Double(myCardDeck.GetNextCard());

            GetNextHand();  // Once you've doubled and you got your card, you're done with this hand
        }

        public void Stand ()
        {
            GetNextHand();
        }

        public void Split()
        {
            players[0].Split(myCardDeck.GetNextCard());
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
    }

    public class Player
    {
        private Money myMoney;
        private List<Hand> myHands;
        private int currentHand;

        MainWindow Form = Application.Current.Windows[0] as MainWindow;

        public Player ()
        {
            currentHand = 0;
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
            myHands.Add(new Hand());
        }

        public Boolean AddCard (Card newCard)
        {
            myHands[currentHand].Add(newCard);
            Form.Hand1.Text = myHands[currentHand].getDisplayText();

            if (myHands[currentHand].value > 21)
            {
                Form.ResultText.Text = "BUST";
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
                myMoney.UpdatePot(0);
            }
            else if (result == PlayResult.winner)
            {
                Form.ResultText.Text = "WINNER!";
                myMoney.UpdatePot(bet);
            }
            else if (result == PlayResult.loser)
            {
                Form.ResultText.Text = "LOSER";
                myMoney.UpdatePot(0 - bet);
            }
            else if (result == PlayResult.blackjack)
            {
                Form.ResultText.Text = "BLACKJACK";
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
        }

        public void Split (Card newCard)
        {
            Card splitCard;

            splitCard = myHands[currentHand].handCards[1];
            myHands[currentHand].Remove(1);

            myHands[currentHand].Add(newCard); // Add the new card to the first hand

            // Creat a new hand and add the split off card to it
            myHands.Add(new Hand());
            myHands[myHands.Count-1].Add(splitCard);

            Form.Hand1.Text = myHands[currentHand].getDisplayText();
        }
        public Boolean PlayNextHand()
        {
            if (myHands.Count == currentHand + 1)
                return false;  // no more hands to play
            else
            {
                currentHand++;

                Form.Hand1.Text = myHands[currentHand].getDisplayText();

                return true;
            }
        }

        public void TrueUp (int dealerValue)
        {
            // see whether we won or lost each hand

            for (int i = 0; i < myHands.Count; i++)
            {
                if (myHands[i].value > 21)
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

        MainWindow Form = Application.Current.Windows[0] as MainWindow;

        public Hand ()
        {
            handCards = new List<Card>();
            value = 0;
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
        
        // Constructor
        public Card (CardRank rank, Suit suit)
        {
            this.suit = suit;
            this.rank = rank;

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
