using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MDP
{

    public partial class Form1 : Form
    {
        float[,] states;//rewards - initial utility
        float[,] utility;
        float[,] prevutility;
        float[] eightNeighbours = new float[8];
        float[] expectedValues = new float[8];
        float alpha = 0.2f;
        int timeStep = 0;
        int rightTarget = 20, downTarget = 20;
        struct cellIndex
        {
            public int r;
            public int c;
        };
        cellIndex[] Policy;
        cellIndex stateIndex;
        cellIndex workingSpace;
        cellIndex[] eightIndices = new cellIndex[8];
        cellIndex target_centre = new cellIndex();
        float target_centreX, target_centreY;
        float min_dist_target = 32000f;

        int startR, startC;
        int N = 1;
        // Instantiate random number generator.  
        private readonly Random _random = new Random();

        // Generates a random number within a range.      
        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }
        public Form1()
        {
            InitializeComponent();
        }
        public void circumspect()
        {
            int block_row = 0, block_col = 0;
            block_row = target_centre.r - 5;
            block_col = target_centre.c - 2;

            for (int i = block_row-1; i < block_row + 11; i++)
            {
                for (int j = block_col-1; j < block_col + 6; j++)
                {
                    states[i, j] += 50;

                }
            }
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    utility [i,j] =  states[i, j] ;
                    
                }
            }

            findNaivePolicy(startR, startC);
            
            updateCell(); 
            
            //Policy highlight
            int newR, newC, new2r, new2c;
            double dist = 0;
            int steps = 0;
            float cx, cy, prevcx, prevcy;
            min_dist_target = 32000f;


            stateIndex.r = startR;
            stateIndex.c = startC;

            prevcx = (stateIndex.r + 1) * (workingSpace.r / N) - (workingSpace.r / (2 * N));
            prevcy = (stateIndex.c + 1) * (workingSpace.c / N) - (workingSpace.c / (2 * N));

           while (states[stateIndex.r, stateIndex.c] < 500)
            {
                dataGridView1.Rows[stateIndex.r].Cells[stateIndex.c].Style.BackColor = Color.White;
                newR = Policy[N * stateIndex.r + stateIndex.c].r;
                newC = Policy[N * stateIndex.r + stateIndex.c].c;
                new2r = Policy[N * newR + newC].r;
                new2c = Policy[N * newR + newC].c;

                ////////SMOOTHING DIRECTION CHANGE///////
                if ((stateIndex.r == new2r) && (newR != new2r) && (Math.Abs(stateIndex.c - newC) == 1) && (Math.Abs(new2c - newC) == 1))
                {
                    if (states[newR, newC] == states[new2r, newC])
                        newR = new2r;
                }
                if ((stateIndex.c == new2c) && (newC != new2c) && (Math.Abs(stateIndex.r - newR) == 1) && (Math.Abs(new2r - newR) == 1))
                {
                    if (states[newR, newC] == states[newR, new2c])
                        newC = new2c;
                }

                ////////////////////////////////////////


                stateIndex.r = newR;
                stateIndex.c = newC;
                cx = (float)((float)stateIndex.r + 1) * ((float)workingSpace.r / N) - ((float)workingSpace.r / (2 * N));
                cy = (float)((float)stateIndex.c + 1) * ((float)workingSpace.c / N) - ((float)workingSpace.c / (2 * N));
                dist += Math.Sqrt((cx - prevcx) * (cx - prevcx) + (cy - prevcy) * (cy - prevcy));
                if (Math.Sqrt((cx - target_centreX) * (cx - target_centreX) + (cy - target_centreY) * (cy - target_centreY)) < min_dist_target)
                    min_dist_target = (float)Math.Sqrt((cx - target_centreX) * (cx - target_centreX) + (cy - target_centreY) * (cy - target_centreY));
                steps++;
                prevcx = cx;
                prevcy = cy;
                //MessageBox.Show(newR.ToString() + "," + newC.ToString());

            }
            //MessageBox.Show(steps.ToString() + ", Total Distance: " + ((int)dist).ToString());
            writedata(((float)trackBar1.Value / 1000).ToString() + "," + dist.ToString() + "," + min_dist_target.ToString(), "MDPdist.txt");
            
        }

        public void findNaivePolicy(int r, int c)
        {

            int nextR = 0, nextC = 0;
            label1:
            if (r < downTarget)
                nextR = r + 1;
            if (r > downTarget)
                nextR = r - 1;

            if (c < rightTarget)
                nextC = c + 1;
            if (c > rightTarget)
                nextC = c - 1;
            if (states[nextR, nextC] <0)
            {
                //Circumvent Block
                if (states[r + 1, c] < 0 || states[r + 1, c + 1] < 0 || states[r - 1, c] < 0 || states[r + 1, c - 1] < 0)
                {
                    nextC = c + 1;
                    nextR = r;
                }
                else if (states[r, c + 1] < 0)
                {
                    nextR = r + 1;
                    nextC = c;
                }
                    
                else if (states[r, c - 1] < 0)
                {
                    nextR = r - 1;
                    nextC = c;
                }
                    
            }
            Policy[N * r + c].r = nextR;
            Policy[N * r + c].c = nextC;

            r = nextR;
            c = nextC;
           //  MessageBox.Show(r.ToString() + "," + c.ToString());
           // dataGridView1.Rows[r].Cells[c].Style.BackColor = Color.Yellow;
            if ((nextR != downTarget) || (nextC != rightTarget))
                goto label1;

           
        }

        public void valueIteration()
        {
            //Initialize utility
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    utility[i, j] = states[i, j]; //Initialization of utility with rewards 
                }
            }

            for (int itr = 1; itr < 901; itr++)
            {
                //Value Iteration
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        utility[i, j] = states[i, j] + 0.6f * max(i, j);

                    }
                }
            }

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    states[i, j] = utility[i, j] + alpha * prevutility[i, j];
                    prevutility[i,j] = states[i, j];
                }
            }

            updateCell();

            //Policy highlight
            int newR, newC, new2r,new2c;
            double dist = 0;
            int steps = 0;
            float cx, cy, prevcx, prevcy;
            min_dist_target = 32000f;


            stateIndex.r = startR;
            stateIndex.c = startC;

            prevcx = (stateIndex.r + 1) * (workingSpace.r / N) - (workingSpace.r / (2 * N));
            prevcy = (stateIndex.c + 1) * (workingSpace.c / N) - (workingSpace.c / (2 * N));
            writedata(("---------Start of Path-------------------"), "MDPpath.txt");
            while (states[stateIndex.r, stateIndex.c] < 1000)
            {
                dataGridView1.Rows[stateIndex.r].Cells[stateIndex.c].Style.BackColor = Color.White;
                newR = Policy[N * stateIndex.r + stateIndex.c].r;
                newC = Policy[N * stateIndex.r + stateIndex.c].c;
                new2r = Policy[N * newR + newC].r;
                new2c = Policy[N * newR + newC].c;
                
                ////////SMOOTHING DIRECTION CHANGE///////
                if ((stateIndex.r==new2r)&&(newR!=new2r)&&(Math.Abs(stateIndex.c-newC)==1)&& (Math.Abs(new2c-newC)==1))
                {
                    if (states[newR, newC] == states[new2r, newC])
                        newR = new2r;
                }
                if ((stateIndex.c == new2c) && (newC != new2c) && (Math.Abs(stateIndex.r - newR) == 1) && (Math.Abs(new2r - newR) == 1))
                {
                    if (states[newR, newC] == states[newR, new2c])
                        newC = new2c;
                }

                ////////////////////////////////////////

                
                stateIndex.r = newR;
                stateIndex.c = newC;
                cx = (float)((float)stateIndex.r + 1) * ((float)workingSpace.r / N) - ((float)workingSpace.r / (2 * N));
                cy = (float)((float)stateIndex.c + 1) * ((float)workingSpace.c / N) - ((float)workingSpace.c / (2 * N));
                writedata((stateIndex.r.ToString()+","+ stateIndex.c.ToString()+","+ cx.ToString() + "," + cy.ToString()), "MDPpath.txt");
                dist += Math.Sqrt((cx - prevcx) * (cx - prevcx) + (cy - prevcy) * (cy - prevcy));
                if (Math.Sqrt((cx - target_centreX) * (cx - target_centreX) + (cy - target_centreY) * (cy - target_centreY))<min_dist_target)
                    min_dist_target = (float)Math.Sqrt((cx - target_centreX) * (cx - target_centreX) + (cy - target_centreY) * (cy - target_centreY));
                steps++;
                prevcx = cx;
                prevcy = cy;
            }
            writedata(("---------End of Path-------------------"), "MDPpath.txt");
            //MessageBox.Show(steps.ToString() + ", Total Distance: " + ((int)dist).ToString());
            writedata(((float)trackBar1.Value / 1000).ToString()+","+dist.ToString()+","+min_dist_target.ToString(), "MDPdist.txt");
        }
        public float max(int r, int c)
        {
            float maxI = -1000;
            int index = 0;
            //int maxK = -10;
            float a = (float)trackBar1.Value / 1000;
            cellIndex maxActionIndex = new cellIndex();


            if (r > 0 && c > 0 && r < N - 1 && c < N - 1)
            {
                index = 0;
                maxI = -1000;

                /////////////////////////////////////////
                ///TARGET IS LEFT OR UP OF SOURCE///////
                ////////////////////////////////////////
                if ((startR > downTarget || startC > rightTarget) && (startC != rightTarget) && (startR != downTarget))

                {
                    for (int i = r - 1; i < r + 2; i++)
                        for (int j = c - 1; j < c + 2; j++)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }
                }

                else if ((startR > downTarget) && (startC == rightTarget) )
                {
                    for (int i = r - 1; i < r + 2; i++)
                        for (int j = c ; j < c + 2; j++)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }
                    for (int i = r - 1; i < r + 2; i++)
                        for (int j = c-1; j < c ; j++)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }
                }

                else if ((startC > rightTarget) && (startR == downTarget))
                    {

                    for (int i = r ; i < r + 2; i++)
                        for (int j = c-1; j < c + 2; j++)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }
                    for (int i = r - 1; i < r ; i++)
                        for (int j = c - 1; j < c+2; j++)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }
                }
                /////////////////////////////////////////
                ///TARGET IS RIGHT OF SOURCE///////
                    ////////////////////////////////////////
                else if ((startC < rightTarget) && (startR != downTarget))
                {

                    for (int i = r - 1; i < r + 2; i++)
                        for (int j = c + 1; j > c - 2; j--)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }

                }

                else if ((startC < rightTarget) && (startR == downTarget))
                {

                    for (int i = r; i < r + 2; i++)
                        for (int j = c+1; j > c - 2; j--)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }
                    for (int i = r-1; i < r ; i++)
                        for (int j = c + 1; j > c - 2; j--)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }

                }

                /////////////////////////////////////////
                ///TARGET IS DOWN OF SOURCE///////
                ////////////////////////////////////////
                else if ((startR < downTarget) && (startC != rightTarget))
                {
                    for (int i = r + 1; i > r - 2; i--)
                        for (int j = c - 1; j < c + 2; j++)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }

                }
                else if ((startR < downTarget) && (startC == rightTarget))
                {
                    for (int i = r+1; i > r - 2; i--)
                        for (int j = c ; j < c + 2; j++)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }
                    for (int i = r + 1; i > r - 2; i--)
                        for (int j = c-1; j < c ; j++)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }
                }

                else
                {
                    for (int i = r + 1; i > r - 2; i--)
                        for (int j = c + 1; j > c - 2; j--)
                        {
                            if (!(i == r && j == c))
                            {
                                eightNeighbours[index] = utility[i, j];
                                eightIndices[index].r = i;
                                eightIndices[index].c = j;
                                index++;
                            }
                        }
                }


                for (int k = 1; k < 7; k++)
                    expectedValues[k] = (float)(a * eightNeighbours[k - 1] + (1 - 2 * a) * eightNeighbours[k] + a * eightNeighbours[k + 1]);
                expectedValues[0] = (float)(a * eightNeighbours[7] + (1 - 2 * a) * eightNeighbours[0] + a * eightNeighbours[1]);
                expectedValues[7] = (float)(a * eightNeighbours[6] + (1 - 2 * a) * eightNeighbours[7] + a * eightNeighbours[0]);

                for (int k = 0; k < 8; k++)
                {
                   // maxI += expectedValues[k];
                    if (expectedValues[k] > maxI)
                    {
                        maxI = expectedValues[k];
                        maxActionIndex.r = eightIndices[k].r;
                        maxActionIndex.c = eightIndices[k].c;
                    }
                }
               // maxI = maxI / 8;
            }

            //Edges
            else if (r > 0 && c == 0 && r < N - 1 && c < N - 1)
            {
                for (int i = r - 1; i < r + 2; i++)
                    for (int j = c; j < c + 2; j++)
                    {
                        if (!(i == r && j == c))
                        {
                            if (utility[i, j] > maxI)
                            {
                                maxI = utility[i, j];
                                maxActionIndex.r = i;
                                maxActionIndex.c = j;
                            }
                        }
                    }
            }

            else if (r == 0 && c > 0 && r < N - 1 && c < N - 1)
            {

                for (int i = r; i < r + 2; i++)
                    for (int j = c - 1; j < c + 2; j++)
                    {
                        if (!(i == r && j == c))
                        {
                            if (utility[i, j] > maxI)
                            {
                                maxI = utility[i, j];
                                maxActionIndex.r = i;
                                maxActionIndex.c = j;
                            }
                        }
                    }
            }
            else if (r > 0 && c > 0 && r < N - 1 && c == N - 1)
            {

                for (int i = r - 1; i < r + 2; i++)
                    for (int j = c - 1; j < c; j++)
                    {
                        if (!(i == r && j == c))
                        {
                            if (utility[i, j] > maxI)
                            {
                                maxI = utility[i, j];
                                maxActionIndex.r = i;
                                maxActionIndex.c = j;
                            }
                        }
                    }
            }
            else if (r > 0 && c > 0 && r == N - 1 && c < N - 1)
            {

                for (int i = r - 1; i < r; i++)
                    for (int j = c - 1; j < c + 2; j++)
                    {
                        if (!(i == r && j == c))
                        {
                            if (utility[i, j] > maxI)
                            {
                                maxI = utility[i, j];
                                maxActionIndex.r = i;
                                maxActionIndex.c = j;
                            }
                        }
                    }
            }
            /// FOUR CORNER POINTS
            else if (r == 0 && c == 0)
            {
                for (int i = 0; i < r + 2; i++)
                    for (int j = 0; j < c + 2; j++)

                    {
                        if (!(i == r && j == c))
                        {
                            if (utility[i, j] > maxI)
                            {
                                maxI = utility[i, j];
                                maxActionIndex.r = i;
                                maxActionIndex.c = j;
                            }
                        }
                    }
            }

            else if (r == 0 && c == N - 1)
            {
                for (int i = 0; i < r + 2; i++)
                    for (int j = c - 1; j < c; j++)

                    {
                        if (!(i == r && j == c))
                        {
                            if (utility[i, j] > maxI)
                            {
                                maxI = utility[i, j];
                                maxActionIndex.r = i;
                                maxActionIndex.c = j;
                            }
                        }
                    }
            }

            else if (r == N - 1 && c == N - 1)
            {
                for (int i = r - 1; i < r; i++)
                    for (int j = c - 1; j < c; j++)

                    {
                        if (!(i == r && j == c))
                        {
                            if (utility[i, j] > maxI)
                            {
                                maxI = utility[i, j];
                                maxActionIndex.r = i;
                                maxActionIndex.c = j;
                            }
                        }
                    }
            }
            else if (r == N - 1 && c == 0)
            {
                for (int i = r - 1; i < r; i++)
                    for (int j = 0; j < c + 2; j++)

                    {
                        if (!(i == r && j == c))
                        {
                            if (utility[i, j] > maxI)
                            {
                                maxI = utility[i, j];
                                maxActionIndex.r = i;
                                maxActionIndex.c = j;
                            }
                        }
                    }
            }

            Policy[N * r + c].r = maxActionIndex.r;
            Policy[N * r + c].c = maxActionIndex.c;
            //MessageBox.Show(r.ToString() + "," + c.ToString());
            return maxI;
        }
        public void drawGrid(int steps)
        {
            states = new float[steps, steps];
            utility = new float[steps, steps];
            prevutility = new float[steps, steps];
            Policy = new cellIndex[N * N];
            stateIndex = new cellIndex();
            dataGridView1.RowCount = steps;
            dataGridView1.ColumnCount = steps;
            dataGridView1.Refresh();

            dataGridView2.RowCount = steps;
            dataGridView2.ColumnCount = steps;
            dataGridView2.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Number of States
            N = 20;

            //Working Space Dimension
            workingSpace = new cellIndex();
            workingSpace.r = 400;
            workingSpace.c = 500;


            drawGrid(N);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    //Uniform Distribution
                    states[i, j] = (1000 / (N * N));

                    //Random Initializtion
                    //states[i, j] = RandomNumber(1, 9);

                }
            }
            updateCell();
            button2.Enabled = true;
        }
        public void updateCell()
        {
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    //Uniform Distribution
                    dataGridView1.Rows[i].Cells[j].Value = (int)states[i, j];
                    if (states[i, j] == 0)
                        dataGridView1.Rows[i].Cells[j].Style.BackColor = Color.Azure;
                    else if (states[i, j] < 0)
                        dataGridView1.Rows[i].Cells[j].Style.BackColor = Color.Red;
                    else if (states[i, j] > 49)
                        dataGridView1.Rows[i].Cells[j].Style.BackColor = Color.FromArgb((int)(0.1 * states[i, j]), 255, 75);
                    else
                        dataGridView1.Rows[i].Cells[j].Style.BackColor = Color.FromArgb((int)(5 * states[i, j]), 75, 255);
                }
            }
        }
        public void updateCell2()
        {
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    //Uniform Distribution
                   
                   dataGridView2.Rows[i].Cells[j].Value = (int)states[i, j];
                   
                    if (states[i, j] == 0)
                        dataGridView2.Rows[i].Cells[j].Style.BackColor = Color.Azure;
                    else if (states[i, j] < 0)
                        dataGridView2.Rows[i].Cells[j].Style.BackColor = Color.Red;
                    else if (states[i, j] > 100)
                        dataGridView2.Rows[i].Cells[j].Style.BackColor = Color.FromArgb((int)(0.1 * states[i, j]), 255, 75);
                    else
                        dataGridView2.Rows[i].Cells[j].Style.BackColor = Color.FromArgb((int)(5 * states[i, j]), 75, 255);
                }
            }
        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // MessageBox.Show(e.RowIndex.ToString()+","+ e.ColumnIndex.ToString());
            if (radioButton1.Checked) //Source
            {
                states[e.RowIndex, e.ColumnIndex] = 0;
                startR = e.RowIndex;
                startC = e.ColumnIndex;
            }
            if (radioButton2.Checked) //Destination
            {
                states[e.RowIndex, e.ColumnIndex] = 600;
                if (e.RowIndex < downTarget)
                    downTarget = e.RowIndex;
                if (e.ColumnIndex < rightTarget)
                    rightTarget = e.ColumnIndex;
            }
            if (radioButton3.Checked)//Blocks
            {
                states[e.RowIndex, e.ColumnIndex] = -500;
            }
            if (radioButton4.Checked)//Uniform
            {
                states[e.RowIndex, e.ColumnIndex] = (1000 / (N * N));
            }
            
            updateCell();
            if (radioButton5.Checked)
            {
                target_centre.r = e.RowIndex;
                target_centre.c = e.ColumnIndex;
                target_centreX = (float)(((float)target_centre.r + 1) * ((float)workingSpace.r / N) - ((float)workingSpace.r / (2 * N)));
                target_centreY = (float)(((float)target_centre.c + 1) * ((float)workingSpace.c / N) - ((float)workingSpace.c / (2 * N)));
                //MessageBox.Show(target_centre.r.ToString()+","+ target_centre.c+" - "+ target_centreX.ToString() + "," + target_centreY.ToString());
               
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            label4.Text = timeStep.ToString();
            updateCell2();
            valueIteration();
            timeStep++;
            button2.Enabled = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
           
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            
            states = new float[N, N];
            utility = new float[N, N];
            Policy = new cellIndex[N * N];
            stateIndex = new cellIndex();

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    //Uniform Distribution
                    states[i, j] = float.Parse(dataGridView2.Rows[i].Cells[j].Value.ToString());

                    //Random Initializtion
                    //states[i, j] = RandomNumber(1, 9);

                }
            }
            updateCell();
            button2.Enabled = true;
        }

        private void button3_Click_2(object sender, EventArgs e)
        {
            //Number of States
            N = 20;

            //Working Space Dimension
            workingSpace = new cellIndex();
            workingSpace.r = 400;
            workingSpace.c = 500;

            drawGrid(N);

            openFileDialog1.InitialDirectory = Application.ExecutablePath;
            openFileDialog1.FileName = "MDPGrid.csv";
            openFileDialog1.ShowDialog();

            string path = openFileDialog1.FileName;

            string[] lines = System.IO.File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string[] columns = line.Split(',');
                foreach (string column in columns)
                {
                    // Do something
                    states[Int32.Parse(columns[0]), Int32.Parse(columns[1])] =float.Parse(columns[2]);//Row, Column Interchanged
                    if (states[Int32.Parse(columns[0]), Int32.Parse(columns[1])] ==0)
                    {
                        startR = Int32.Parse(columns[0]);
                        startC = Int32.Parse(columns[1]);
                    }
                }
            }


            updateCell();
            button2.Enabled = true;


        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            float f = (float)trackBar1.Value / 1000;
            label2.Text = f.ToString();
          
        }

        private void button4_Click(object sender, EventArgs e)
        {

            int block_row = 0, block_col = 0;
            int source_row = 0, source_column = 0;
            int target_row = 0, target_column = 0;
            bool flag = true;
            
            Random r=new Random();
            //Number of States
            N = 20;

            //Working Space Dimension
            workingSpace = new cellIndex();
            workingSpace.r = 400;
            workingSpace.c = 500;
            for (int itr = 1; itr < 6; itr++)
            {

                drawGrid(N);


                block_row = r.Next(1, 10);
                block_col = r.Next(4, 10);

                for (int i = block_row; i < block_row + 10; i++)
                {
                    for (int j = block_col; j < block_col + 5; j++)
                    {
                        states[i, j] = -500;

                    }
                }
                target_centre.r = block_row + 5;
                target_centre.c = block_col + 2;
                target_centreX = (float)((float)target_centre.r + 1) * ((float)workingSpace.r / N) - ((float)workingSpace.r / (2 * N));
                target_centreY = (float)((float)target_centre.c + 1) * ((float)workingSpace.c / N) - ((float)workingSpace.c / (2 * N));

                if (flag == true)
                {
                    source_row = r.Next(1, 19);
                    source_column = r.Next(1, 4);
                    target_row = r.Next(1, 19);
                    target_column = r.Next(15, 19);


                }
                else
                {
                    source_row = r.Next(1, 19);
                    source_column = r.Next(15, 19);
                    target_row = r.Next(1, 19);
                    target_column = r.Next(1, 4);

                }
                states[source_row, source_column] = 0;
                states[target_row, target_column] = 600;
                states[target_row, target_column + 1] = 600;
                startR = source_row;
                startC = source_column;
                flag = !flag;



                updateCell();
                button2_Click(sender, e);
                timeStep = 0;
                
               
                drawGrid(N);
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        states[i, j] = 2;

                    }
                }

                updateCell();
                updateCell2();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            circumspect();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            timeStep = 0;


            drawGrid(N);
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    states[i, j] = 2;

                }
            }

            updateCell();
            updateCell2();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {

        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            button3_Click(sender, e);
        }
        private static void writedata(string data, string filename)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@filename, true))
            {
                file.WriteLine(data + "," + System.Environment.TickCount.ToString());
            }

        }
        private void reset()
        {

        }
    }


         
}
