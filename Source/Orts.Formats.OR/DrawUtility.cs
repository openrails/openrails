// COPYRIGHT 2014, 2015 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using Orts.Formats.Msts;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Orts.Formats.OR
{
    public static class DrawUtility
    {
        // Calculate the distance between
        // point pt and the segment startPoint --> middlePoint.

        public static double FindDistancePoints(PointF point1, PointF point2)
        {
            double somme = Math.Pow((point2.X - point1.X), 2) + Math.Pow((point2.Y - point1.Y), 2);
            somme = Math.Sqrt(somme);

            return somme;
        }

        public static double FindDistanceToSegment(PointF pt, TrackSegment segment, out PointF closest)
        {
            if (!segment.isCurved)
            {
                return FindDistanceToSegment(pt,
                    new AESegment (segment.associateSegment),
                    out closest);
            }
            else
            {
                return FindDistanceToCurve (pt, new AESegment (segment.associateSegment), out closest);
            }
        }

        public static double FindDistanceToSegment(PointF pt, AESegment seg, out PointF closest)
        {
            float dx = seg.endPoint.X - seg.startPoint.X;
            float dy = seg.endPoint.Y - seg.startPoint.Y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = seg.startPoint;
                dx = pt.X - seg.startPoint.X;
                dy = pt.Y - seg.startPoint.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            float t = ((pt.X - seg.startPoint.X) * dx + (pt.Y - seg.startPoint.Y) * dy) / (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.

            if (t < 0)
            {
                closest = new PointF (seg.startPoint.X, seg.startPoint.Y);
                dx = pt.X - seg.startPoint.X;
                dy = pt.Y - seg.startPoint.Y;
            }
            else if (t > 1)
            {
                closest = new PointF (seg.endPoint.X, seg.endPoint.Y);
                dx = pt.X - seg.endPoint.X;
                dy = pt.Y - seg.endPoint.Y;
            }
            else
            {
                closest = new PointF(seg.startPoint.X + t * dx, seg.startPoint.Y + t * dy);
                dx = pt.X - closest.X;
                dy = pt.Y - closest.Y;
            }
            double info = Math.Sqrt (dx * dx + dy * dy);
            return info;
        }

        // Find the point of intersection between
        // the lines startPoint --> middlePoint and endPoint --> p4.
        public static void FindIntersection(AESegment segArea, AESegment track,
            out bool lines_intersect, out bool segments_intersect,
            out PointF intersection, out PointF close_p1, out PointF close_p2)
        {
            // Get the segments' parameters.
            float dx12 = segArea.endPoint.X - segArea.startPoint.X;
            float dy12 = segArea.endPoint.Y - segArea.startPoint.Y;
            float dx34 = track.endPoint.X - track.startPoint.X;
            float dy34 = track.endPoint.Y - track.startPoint.Y;

            // Solve for t1 and t2
            float denominator = (dy12 * dx34 - dx12 * dy34);

            float t1;
            try
            {
                t1 = ((segArea.startPoint.X - track.startPoint.X) * dy34 + (track.startPoint.Y - segArea.startPoint.Y) * dx34) / denominator;
            }
            catch
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new PointF(float.NaN, float.NaN);
                close_p1 = new PointF(float.NaN, float.NaN);
                close_p2 = new PointF(float.NaN, float.NaN);
                return;
            }
            lines_intersect = true;

            float t2 = ((track.startPoint.X - segArea.startPoint.X) * dy12 + (segArea.startPoint.Y - track.startPoint.Y) * dx12) / -denominator;

            // Find the point of intersection.
            intersection = new PointF (segArea.startPoint.X + dx12 * t1, segArea.startPoint.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect = ((t1 >= 0) && (t1 <= 1) && (t2 >= 0) && (t2 <= 1));

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new PointF (segArea.startPoint.X + dx12 * t1, segArea.startPoint.Y + dy12 * t1);
            close_p2 = new PointF (track.startPoint.X + dx34 * t2, track.startPoint.Y + dy34 * t2);
        }


        public static PointF FindIntersection(AESegment segArea, AESegment track)
        {
            PointF intersection;
            if (track.isCurved)
            {
                if ((int)(track.startPoint.X) == 45892)
                    intersection = PointF.Empty;
                intersection = DrawUtility.FindCurveIntersection(segArea, track);
            }
            else
            {
                intersection = DrawUtility.FindStraightIntersection(segArea, track);
            }
            return intersection;
        }

        //  public domain function by Darel Rex Finley, 2006
        //  Determines the intersection point of the line segment defined by points A and B
        //  with the line segment defined by points C and D.
        //
        //  Returns YES if the intersection point was found, and stores that point in X,Y.
        //  Returns NO if there is no determinable intersection point, in which case X,Y will
        //  be unmodified.

        public static PointF FindStraightIntersection(AESegment segArea, AESegment track)
        {
//double Ax, double Ay,
//double Bx, double By,
//double Cx, double Cy,
//double Dx, double Dy,
//double *X, double *Y 
            PointF pt = PointF.Empty;
            double  distAB, theCos, theSin, newX, ABpos ;
            double distCD, theCos2, theSin2;
            double ABX, ABY, ACX, ACY, ADX, ADY;
            double AX = segArea.startPoint.X;
            double AY = segArea.startPoint.Y;
            double CDX, CDY, angle1, angle2;

            //  Fail if either line segment is zero-length.
            if ((segArea.startPoint.X == segArea.endPoint.X && segArea.startPoint.Y == segArea.endPoint.Y) 
                || (track.startPoint.X == track.endPoint.X && track.startPoint.Y == track.endPoint.Y))
                return pt;

            //  Fail if the segments share an end-point.
            if ((segArea.startPoint.X == track.startPoint.X && segArea.startPoint.Y == track.startPoint.Y) ||
                (segArea.endPoint.X == track.startPoint.X && segArea.endPoint.Y == track.startPoint.Y) ||
                (segArea.startPoint.X == track.endPoint.X && segArea.startPoint.Y == track.endPoint.Y) ||
                (segArea.endPoint.X == track.endPoint.X && segArea.endPoint.Y == track.endPoint.Y))
            {
                return pt; 
            }

            //  (1) Translate the system so that point A is on the origin.
            ABX = segArea.endPoint.X - segArea.startPoint.X;
            ABY = segArea.endPoint.Y - segArea.startPoint.Y;
            ACX = track.startPoint.X - segArea.startPoint.X;
            ACY = track.startPoint.Y - segArea.startPoint.Y;
            ADX = track.endPoint.X - segArea.startPoint.X;
            ADY = track.endPoint.Y - segArea.startPoint.Y;
            CDX = track.endPoint.X - track.startPoint.X;
            CDY = track.endPoint.Y - track.startPoint.Y;

            //  Discover the length of segment A-B.
            distAB = Math.Sqrt(ABX * ABX + ABY * ABY);
            distCD = Math.Sqrt(CDX * CDX + CDY * CDY);  // sup

            //  (2) Rotate the system so that point B is on the positive X axis.
            theCos = ABX / distAB;
            theSin = ABY / distAB;

            theCos2 = (CDX) / distCD;   // sup
            theSin2 = (CDY) / distCD;   // sup
            angle1 = Math.Acos(theCos) * 180 / Math.PI;   // sup
            angle2 = Math.Acos(theCos2) * 180 / Math.PI;   // sup
            newX = ACX * theCos + ACY * theSin;
            ACY  = ACY * theCos - ACX * theSin; 
            ACX = newX;
            newX = ADX * theCos + ADY * theSin;
            ADY  = ADY * theCos - ADX * theSin; 
            ADX = newX;

            if (Math.Abs(angle1 - angle2) < 5)   // sup
                return pt;   // sup
            //  Fail if segment C-D doesn't cross line A-B.
            if (ACY < 0 && ADY < 0 || ACY >= 0 && ADY >= 0) 
                return pt;

            //  (3) Discover the position of the intersection point along line A-B.
            ABpos = ADX + (ACX - ADX) * ADY / (ADY - ACY);

            //  Fail if segment C-D crosses line A-B outside of segment A-B.
            if (ABpos < 0 || ABpos > distAB) 
                return pt;

            //  (4) Apply the discovered position to line A-B in the original coordinate system.
            pt.X = (float)(AX + ABpos * theCos);
            pt.Y = (float)(AY + ABpos * theSin);
            if (FindDistancePoints(pt, segArea.startPoint) < 0.1 || FindDistancePoints(pt, segArea.endPoint) < 0.1)
                return PointF.Empty;
            //  Success.
            return pt;
        }
#if false
		        public static PointF FindStraightIntersection(AESegment segArea, AESegment track)
        {
            float xD1, yD1, xD2, yD2, xD3, yD3;
            double dot, deg;
            double len1, len2;
            double segmentLen1, segmentLen2;
            float ua, ub, div;

            // calculate differences  
            xD1 = segArea.endPoint.X - segArea.startPoint.X;
            xD2 = track.endPoint.X - track.startPoint.X;
            yD1 = segArea.endPoint.Y - segArea.startPoint.Y;
            yD2 = track.endPoint.Y - track.startPoint.Y;
            xD3 = segArea.startPoint.X - track.startPoint.X;
            yD3 = segArea.startPoint.Y - track.startPoint.Y;

            // calculate the lengths of the two lines  
            len1 = Math.Sqrt(xD1 * xD1 + yD1 * yD1);
            len2 = Math.Sqrt(xD2 * xD2 + yD2 * yD2);

            // calculate angle between the two lines.  
            dot = (xD1 * xD2 + yD1 * yD2); // dot product  
            deg = dot / (len1 * len2);

            // if abs(angle)==1 then the lines are parallell,  
            // so no intersection is possible  
            if (Math.Abs(deg) == 1) 
=======
            // find intersection Pt between two lines    
            PointF pt = new PointF(0, 0);
            div = yD2 * xD1 - xD2 * yD1;
            if (div == 0)
>>>>>>> .r37
                return PointF.Empty;
<<<<<<< .mine
=======
            ua = (xD2 * yD3 - yD2 * xD3) / div;
            ub = (xD1 * yD3 - yD1 * xD3) / div;
            pt.Y = segArea.startPoint.Y + ub * yD1;
            pt.X = segArea.startPoint.X + ua * xD1;
            pt.Y = segArea.startPoint.Y + ua * yD1;
>>>>>>> .r37

<<<<<<< .mine
            // find intersection Pt between two lines    
            PointF pt = new PointF(0, 0);
            div = yD2 * xD1 - xD2 * yD1;
            if (div == 0)
                return PointF.Empty;
            ua = (xD2 * yD3 - yD2 * xD3) / div;
            ub = (xD1 * yD3 - yD1 * xD3) / div;
            pt.Y = segArea.startPoint.Y + ub * yD1;
            pt.X = segArea.startPoint.X + ua * xD1;
            pt.Y = segArea.startPoint.Y + ua * yD1;
=======
            // calculate the combined length of the two segments  
            // between Pt-p1 and Pt-p2  
            xD1 = pt.X - segArea.startPoint.X;
            xD2 = pt.X - segArea.endPoint.X;
            yD1 = pt.Y - segArea.startPoint.Y;
            yD2 = pt.Y - segArea.endPoint.Y;
            segmentLen1 = Math.Sqrt(xD1 * xD1 + yD1 * yD1) + Math.Sqrt(xD2 * xD2 + yD2 * yD2);
>>>>>>> .r37

<<<<<<< .mine
            // calculate the combined length of the two segments  
            // between Pt-p1 and Pt-p2  
            xD1 = pt.X - segArea.startPoint.X;
            xD2 = pt.X - segArea.endPoint.X;
            yD1 = pt.Y - segArea.startPoint.Y;
            yD2 = pt.Y - segArea.endPoint.Y;
            segmentLen1 = Math.Sqrt(xD1 * xD1 + yD1 * yD1) + Math.Sqrt(xD2 * xD2 + yD2 * yD2);

            // calculate the combined length of the two segments  
            // between Pt-p3 and Pt-p4  
            xD1 = pt.X - track.startPoint.X;
            xD2 = pt.X - track.endPoint.X;
            yD1 = pt.Y - track.startPoint.Y;
            yD2 = pt.Y - track.endPoint.Y;
            segmentLen2 = Math.Sqrt(xD1 * xD1 + yD1 * yD1) + Math.Sqrt(xD2 * xD2 + yD2 * yD2);

            // if the lengths of both sets of segments are the same as  
            // the lenghts of the two lines the point is actually   
            // on the line segment.  

            // if the point isn't on the line, return null  
            if (Math.Abs(len1 - segmentLen1) > 0.01 || Math.Abs(len2 - segmentLen2) > 0.01)
                return PointF.Empty;

            // return the valid intersection  
            if (pt.IsEmpty)
                return PointF.Empty;
            if (FindDistancePoints(pt, segArea.startPoint) < 0.1 || FindDistancePoints(pt, segArea.endPoint) < 0.1)
                return PointF.Empty;
            return pt;
=======
            // calculate the combined length of the two segments  
            // between Pt-p3 and Pt-p4  
            xD1 = pt.X - track.startPoint.X;
            xD2 = pt.X - track.endPoint.X;
            yD1 = pt.Y - track.startPoint.Y;
            yD2 = pt.Y - track.endPoint.Y;
            segmentLen2 = Math.Sqrt(xD1 * xD1 + yD1 * yD1) + Math.Sqrt(xD2 * xD2 + yD2 * yD2);

            // if the lengths of both sets of segments are the same as  
            // the lenghts of the two lines the point is actually   
            // on the line segment.  

            // if the point isn't on the line, return null  
            if (Math.Abs(len1 - segmentLen1) > 0.01 || Math.Abs(len2 - segmentLen2) > 0.01)
                return PointF.Empty;

            // return the valid intersection  
            if (pt.IsEmpty)
                return PointF.Empty;
            if (FindDistancePoints(pt, segArea.startPoint) < 0.1 || FindDistancePoints(pt, segArea.endPoint) < 0.1)
                return PointF.Empty;
            return pt;
>>>>>>> .r37
        }
  
	#endif
        public static PointF FindCurveIntersection(AESegment segArea, AESegment track)
        {
            PointF pointA = track.startPoint;
            PointF pointB = track.endPoint;
            AESegment partTrack;
            PointF intersect;

            for (int i = 1; i < track.step; i++)
            {
                double sub_angle = (i) * track.angleTot / track.step;
                double info2x = track.radius * Math.Cos(track.startAngle + sub_angle);
                double info2y = track.radius * Math.Sin(track.startAngle + sub_angle);
                double dx = (track.center.X + info2x);
                double dy = (track.center.Y + info2y);

                pointB = new PointF((float)dx, (float)dy);
                partTrack = new AESegment(pointA, pointB);
                intersect = FindStraightIntersection(segArea, partTrack);
                if (intersect != PointF.Empty)
                {
                    return intersect;
                }
                pointA = pointB;
            }
            pointB = track.endPoint;
            partTrack = new AESegment(pointA, pointB);
            intersect = FindStraightIntersection(segArea, partTrack);
            if (intersect != PointF.Empty)
            {
                return intersect;
            }

            return PointF.Empty;
        }

        public static double FindDistanceToCurve (PointF pt, AESegment segment, out PointF closest)
        {
            double dist = 0;
            double savedDist = double.PositiveInfinity;
            PointF current = new PointF(0f, 0f);
            if (!segment.isCurved || segment.radius == 0)
                return FindDistanceToSegment (pt, segment, out closest);

            PointF pointA = segment.startPoint;
            PointF pointB = segment.endPoint;
            PointF pointCenter = segment.center;
            closest = current;
            for (int i = 0; i <= segment.step; i++) 
            {
                double sub_angle = ((float)i / segment.step) * segment.angleTot;
                double infox = (1 - Math.Cos(sub_angle)) * (-pointB.X);
                double infoy = (1 - Math.Cos(sub_angle)) * (-pointB.Y);
                double info2x = segment.radius * Math.Cos(segment.startAngle + sub_angle);
                double info2y = segment.radius * Math.Sin(segment.startAngle + sub_angle);
                double dx = pt.X - (pointCenter.X + info2x);
                double dy = pt.Y - (pointCenter.Y + info2y);
                MSTSCoord tempo = new MSTSCoord(new PointF((float)(pointCenter.X + info2x), (float)(pointCenter.Y + info2y)));
                dist = Math.Sqrt(dx * dx + dy * dy);
                current = new PointF((float)(pointCenter.X + info2x), (float)(pointCenter.Y + info2y));
                if (dist < savedDist)
                {
                    savedDist = dist;
                    closest = current;
                }
            }
            savedDist = Math.Round (savedDist, 1);
            return savedDist;
        }

        public static bool PointInPolygon(PointF point, List<System.Drawing.PointF> polyPoints)
        {
            var j = polyPoints.Count - 1;
            var oddNodes = false;

            for (var i = 0; i < polyPoints.Count; i++)
            {
                if (polyPoints[i].Y < point.Y && polyPoints[j].Y >= point.Y ||
                    polyPoints[j].Y < point.Y && polyPoints[i].Y >= point.Y)
                {
                    if (polyPoints[i].X +
                        (point.Y - polyPoints[i].Y) / (polyPoints[j].Y - polyPoints[i].Y) * (polyPoints[j].X - polyPoints[i].X) < point.X)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }

            return oddNodes;
        }

        public static int getDirection (TrackNode fromNode, TrackNode toNode)
        {
            foreach (var pin in fromNode.TrPins)
            {
                if (pin.Link == toNode.Index)
                    return pin.Direction;
            }
            return 0;
        }

    }
}

#if false
		        //Circle to Circle
        static public IntersectionObject CircleToCircleIntersection(float x0_, float y0_, float r0_,
                                                                    float x1_, float y1_, float r1_)
        {
            IntersectionObject result = new IntersectionObject();
            float a, dist, h;
            Vector2 d, r = new Vector2(), v2 = new Vector2();

            //d is the vertical and horizontal distances between the circle centers
            d = new Vector2(x1_ - x0_, y1_ - y0_);

            //distance between the circles
            dist = d.Length();

            //Check for equality and infinite intersections exist
            if (dist == 0 && r0_ == r1_)
            {
                return result;
            }

            //Check for solvability
            if (dist > r0_ + r1_)
            {
                //no solution. circles do not intersect
                return result;
            }
            if (dist < Math.Abs(r0_ - r1_))
            {
                //no solution. one circle is contained in the other
                return result;
            }
            if (dist == r0_ + r1_)
            {
                //one solution
                result.InsertSolution((x0_ - x1_) / (r0_ + r1_) * r0_ + x1_, (y0_ - y1_) / (r0_ + r1_) * r0_ + y1_);
                return result;
            }

            /* 'point 2' is the point where the line through the circle
             * intersection points crosses the line between the circle
             * centers. 
             */

            //Determine the distance from point 0 to point 2
            a = ((r0_ * r0_) - (r1_ * r1_) + (dist * dist)) / (2.0f * dist);

            //Determine the coordinates of point 2
            v2 = new Vector2(x0_ + (d.X * a / dist), y0_ + (d.Y * a / dist));

            //Determine the distance from point 2 to either of the intersection points
            h = (float)Math.Sqrt((r0_ * r0_) - (a * a));

            //Now determine the offsets of the intersection points from point 2
            r = new Vector2(-d.Y * (h / dist), d.X * (h / dist));

            //Determine the absolute intersection points
            result.InsertSolution(v2 + r);
            result.InsertSolution(v2 - r);

            return result;
        }

        //Circle to Line
        static public IntersectionObject CircleToLineIntersection(float x1_, float y1_, float r1_,
                                                                  float x2_, float y2_, float x3_, float y3_)
        {
            return LineToCircleIntersection(x2_, y2_, x3_, y3_, x1_, y1_, r1_);
        }

        //Line to Circle
        static public IntersectionObject LineToCircleIntersection(float x1_, float y1_, float x2_, float y2_,
                                                                  float x3_, float y3_, float r3_)
        {
            IntersectionObject result = new IntersectionObject();
            Vector2 v1, v2;
            //Vector from point 1 to point 2
            v1 = new Vector2(x2_ - x1_, y2_ - y1_);
            //Vector from point 1 to the circle's center
            v2 = new Vector2(x3_ - x1_, y3_ - y1_);

            float dot = v1.X * v2.X + v1.Y * v2.Y;
            Vector2 proj1 = new Vector2(((dot / (v1.LengthSq())) * v1.X), ((dot / (v1.LengthSq())) * v1.Y));
            Vector2 midpt = new Vector2(x1_ + proj1.X, y1_ + proj1.Y);

            float distToCenter = (midpt.X - x3_) * (midpt.X - x3_) + (midpt.Y - y3_) * (midpt.Y - y3_);
            if (distToCenter > r3_ * r3_) return result;
            if (distToCenter == r3_ * r3_)
            {
                result.InsertSolution(midpt);
                return result;
            }
            float distToIntersection;
            if (distToCenter == 0)
            {
                distToIntersection = r3_;
            }
            else
            {
                distToCenter = (float)Math.Sqrt(distToCenter);
                distToIntersection = (float)Math.Sqrt(r3_ * r3_ - distToCenter * distToCenter);
            }
            float lineSegmentLength = 1 / (float)v1.Length();
            v1 *= lineSegmentLength;
            v1 *= distToIntersection;

            result.InsertSolution(midpt + v1);
            result.InsertSolution(midpt - v1);

            return result;
        }

        //Circle to LineSegment
        static public IntersectionObject CircleToLineSegmentIntersection(float x1_, float y1_, float r1_,
                                                                         float x2_, float y2_, float x3_, float y3_)
        {
            return LineSegmentToCircleIntersection(x2_, y2_, x3_, y3_, x1_, y1_, r1_);
        }

        //Circle to Ray
        static public IntersectionObject CircleToRayIntersection(float x1_, float y1_, float r1_,
                                                                 float x2_, float y2_, float x3_, float y3_)
        {
            return RayToCircleIntersection(x2_, y2_, x3_, y3_, x1_, y1_, r1_);
        }

        //Ray to Circle
        static public IntersectionObject RayToCircleIntersection(float x1_, float y1_, float x2_, float y2_,
                                                                 float x3_, float y3_, float r3_)
        {
            IntersectionObject result = new IntersectionObject();
            Vector2 v1, v2;
            //Vector from point 1 to point 2
            v1 = new Vector2(x2_ - x1_, y2_ - y1_);
            //Vector from point 1 to the circle's center
            v2 = new Vector2(x3_ - x1_, y3_ - y1_);

            float dot = v1.X * v2.X + v1.Y * v2.Y;
            Vector2 proj1 = new Vector2(((dot / (v1.LengthSq())) * v1.X), ((dot / (v1.LengthSq())) * v1.Y));

            Vector2 midpt = new Vector2(x1_ + proj1.X, y1_ + proj1.Y);
            float distToCenter = (midpt.X - x3_) * (midpt.X - x3_) + (midpt.Y - y3_) * (midpt.Y - y3_);
            if (distToCenter > r3_ * r3_) return result;
            if (distToCenter == r3_ * r3_)
            {
                result.InsertSolution(midpt);
                return result;
            }
            float distToIntersection;
            if (distToCenter == 0)
            {
                distToIntersection = r3_;
            }
            else
            {
                distToCenter = (float)Math.Sqrt(distToCenter);
                distToIntersection = (float)Math.Sqrt(r3_ * r3_ - distToCenter * distToCenter);
            }
            float lineSegmentLength = 1 / (float)v1.Length();
            v1 *= lineSegmentLength;
            v1 *= distToIntersection;

            Vector2 solution1 = midpt + v1;
            if ((solution1.X - x1_) * v1.X + (solution1.Y - y1_) * v1.Y > 0)
            {
                result.InsertSolution(solution1);
            }
            Vector2 solution2 = midpt - v1;
            if ((solution2.X - x1_) * v1.X + (solution2.Y - y1_) * v1.Y > 0)
            {
                result.InsertSolution(solution2);
            }
            return result;
        }

        //Line to Line
        static public IntersectionObject LineToLineIntersection(float x1_, float y1_, float x2_, float y2_,
                                                                float x3_, float y3_, float x4_, float y4_)
        {
            IntersectionObject result = new IntersectionObject();
            float r, s, d;
            //Make sure the lines aren't parallel
            if ((y2_ - y1_) / (x2_ - x1_) != (y4_ - y3_) / (x4_ - x3_))
            {
                d = (((x2_ - x1_) * (y4_ - y3_)) - (y2_ - y1_) * (x4_ - x3_));
                if (d != 0)
                {
                    r = (((y1_ - y3_) * (x4_ - x3_)) - (x1_ - x3_) * (y4_ - y3_)) / d;
                    s = (((y1_ - y3_) * (x2_ - x1_)) - (x1_ - x3_) * (y2_ - y1_)) / d;

                    result.InsertSolution(x1_ + r * (x2_ - x1_), y1_ + r * (y2_ - y1_));
                }
            }
            return result;
        }

        //LineSegment to LineSegment
        static public IntersectionObject LineSegmentToLineSegmentIntersection(float x1_, float y1_, float x2_, float y2_,
                                                                              float x3_, float y3_, float x4_, float y4_)
        {
            IntersectionObject result = new IntersectionObject();
            float r, s, d;
            //Make sure the lines aren't parallel
            if ((y2_ - y1_) / (x2_ - x1_) != (y4_ - y3_) / (x4_ - x3_))
            {
                d = (((x2_ - x1_) * (y4_ - y3_)) - (y2_ - y1_) * (x4_ - x3_));
                if (d != 0)
                {
                    r = (((y1_ - y3_) * (x4_ - x3_)) - (x1_ - x3_) * (y4_ - y3_)) / d;
                    s = (((y1_ - y3_) * (x2_ - x1_)) - (x1_ - x3_) * (y2_ - y1_)) / d;
                    if (r >= 0 && r <= 1)
                    {
                        if (s >= 0 && s <= 1)
                        {
                            result.InsertSolution(x1_ + r * (x2_ - x1_), y1_ + r * (y2_ - y1_));
                        }
                    }
                }
            }
            return result;
        }

        //Line to LineSement
        static public IntersectionObject LineToLineSegmentIntersection(float x1_, float y1_, float x2_, float y2_,
                                                                       float x3_, float y3_, float x4_, float y4_)
        {
            return LineSegmentToLineIntersection(x3_, y3_, x4_, y4_, x1_, y1_, x2_, y2_);
        }

        //LineSegment to Line
        static public IntersectionObject LineSegmentToLineIntersection(float x1_, float y1_, float x2_, float y2_,
                                                                       float x3_, float y3_, float x4_, float y4_)
        {
            IntersectionObject result = new IntersectionObject();
            float r, s, d;
            //Make sure the lines aren't parallel
            if ((y2_ - y1_) / (x2_ - x1_) != (y4_ - y3_) / (x4_ - x3_))
            {
                d = (((x2_ - x1_) * (y4_ - y3_)) - (y2_ - y1_) * (x4_ - x3_));
                if (d != 0)
                {
                    r = (((y1_ - y3_) * (x4_ - x3_)) - (x1_ - x3_) * (y4_ - y3_)) / d;
                    s = (((y1_ - y3_) * (x2_ - x1_)) - (x1_ - x3_) * (y2_ - y1_)) / d;
                    if (r >= 0 && r <= 1)
                    {
                        result.InsertSolution(x1_ + r * (x2_ - x1_), y1_ + r * (y2_ - y1_));
                    }
                }
            }
            return result;
        }

        //LineSegment to Ray
        static public IntersectionObject LineSegmentToRayIntersection(float x1_, float y1_, float x2_, float y2_,
                                                                      float x3_, float y3_, float x4_, float y4_)
        {
            return RayToLineSegmentIntersection(x3_, y3_, x4_, y4_, x1_, y1_, x2_, y2_);
        }

        //Ray to LineSegment
        static public IntersectionObject RayToLineSegmentIntersection(float x1_, float y1_, float x2_, float y2_,
                                                                      float x3_, float y3_, float x4_, float y4_)
        {
            IntersectionObject result = new IntersectionObject();
            float r, s, d;
            //Make sure the lines aren't parallel
            if ((y2_ - y1_) / (x2_ - x1_) != (y4_ - y3_) / (x4_ - x3_))
            {
                d = (((x2_ - x1_) * (y4_ - y3_)) - (y2_ - y1_) * (x4_ - x3_));
                if (d != 0)
                {
                    r = (((y1_ - y3_) * (x4_ - x3_)) - (x1_ - x3_) * (y4_ - y3_)) / d;
                    s = (((y1_ - y3_) * (x2_ - x1_)) - (x1_ - x3_) * (y2_ - y1_)) / d;
                    if (r >= 0)
                    {
                        if (s >= 0 && s <= 1)
                        {
                            result.InsertSolution(x1_ + r * (x2_ - x1_), y1_ + r * (y2_ - y1_));
                        }
                    }
                }
            }
            return result;
        }

        //Line to Ray
        static public IntersectionObject LineToRayIntersection(float x1_, float y1_, float x2_, float y2_,
                                                               float x3_, float y3_, float x4_, float y4_)
        {
            return RayToLineIntersection(x3_, y3_, x4_, y4_, x1_, y1_, x2_, y2_);
        }

        //Ray to Line
        static public IntersectionObject RayToLineIntersection(float x1_, float y1_, float x2_, float y2_,
                                                               float x3_, float y3_, float x4_, float y4_)
        {
            IntersectionObject result = new IntersectionObject();
            float r, s, d;
            //Make sure the lines aren't parallel
            if ((y2_ - y1_) / (x2_ - x1_) != (y4_ - y3_) / (x4_ - x3_))
            {
                d = (((x2_ - x1_) * (y4_ - y3_)) - (y2_ - y1_) * (x4_ - x3_));
                if (d != 0)
                {
                    r = (((y1_ - y3_) * (x4_ - x3_)) - (x1_ - x3_) * (y4_ - y3_)) / d;
                    s = (((y1_ - y3_) * (x2_ - x1_)) - (x1_ - x3_) * (y2_ - y1_)) / d;
                    if (r >= 0)
                    {
                        result.InsertSolution(x1_ + r * (x2_ - x1_), y1_ + r * (y2_ - y1_));
                    }
                }
            }
            return result;
        }

        //Ray to Ray
        static public IntersectionObject RayToRayIntersection(float x1_, float y1_, float x2_, float y2_,
                                                              float x3_, float y3_, float x4_, float y4_)
        {
            IntersectionObject result = new IntersectionObject();
            float r, s, d;
            //Make sure the lines aren't parallel
            if ((y2_ - y1_) / (x2_ - x1_) != (y4_ - y3_) / (x4_ - x3_))
            {
                d = (((x2_ - x1_) * (y4_ - y3_)) - (y2_ - y1_) * (x4_ - x3_));
                if (d != 0)
                {
                    r = (((y1_ - y3_) * (x4_ - x3_)) - (x1_ - x3_) * (y4_ - y3_)) / d;
                    s = (((y1_ - y3_) * (x2_ - x1_)) - (x1_ - x3_) * (y2_ - y1_)) / d;
                    if (r >= 0)
                    {
                        if (s >= 0)
                        {
                            result.InsertSolution(x1_ + r * (x2_ - x1_), y1_ + r * (y2_ - y1_));
                        }
                    }
                }
            }
            return result;
        }
    }
  
            PointF result = PointF.Empty;
            Vector2 v1, v2;
            //Vector from point 1 to point 2
            v1 = new Vector2(segArea.endPoint.X - segArea.startPoint.X, segArea.endPoint.Y - segArea.startPoint.Y);
            //Vector from point 1 to the circle's center
            v2 = new Vector2(track.center.X - segArea.startPoint.X, track.center.Y - segArea.startPoint.Y);
            float lengthSq = (float)Math.Pow(Math.Sqrt((v1.X * v1.X) + (v1.Y * v1.Y)), 2);
            float dot = (float)(v1.X * v2.X + v1.Y * v2.Y);
            Vector2 proj1 = new Vector2((((dot / lengthSq)) * v1.X), ((dot / (lengthSq)) * v1.Y));

            Vector2 midpt = new Vector2(segArea.startPoint.X + proj1.X, segArea.startPoint.Y + proj1.Y);
            float distToCenter = (float)((midpt.X - track.center.X) * (midpt.X - track.center.X) + (midpt.Y - track.center.Y) * (midpt.Y - track.center.Y));
            if (distToCenter > track.radius * track.radius)
                return result;
            if (distToCenter == track.radius * track.radius)
            {
                return result;  // midp;
            }
            float distToIntersection;
            if (distToCenter == 0)
            {
                distToIntersection = track.radius;
            }
            else
            {
                distToCenter = (float)Math.Sqrt(distToCenter);
                distToIntersection = (float)Math.Sqrt(track.radius * track.radius - distToCenter * distToCenter);
            }
            float lineSegmentLength = 1 / (float)v1.Length();
            v1 *= lineSegmentLength;
            v1 *= distToIntersection;

            Vector2 solution1 = midpt + v1;
            PointF positRecue;
            double dist1 = FindDistanceToCurve(new PointF((float)solution1.X, (float)solution1.Y), track, out positRecue);
            //if ((solution1.X - segArea.endPoint.X) * v1.X + (solution1.Y - segArea.endPoint.Y) * v1.Y > 0 &&
            //    (solution1.X - segArea.startPoint.X) * v1.X + (solution1.Y - segArea.startPoint.Y) * v1.Y < 0)
            if (dist1 <= 0.1)
            {
                result = new PointF((float)solution1.X, (float)solution1.Y);  // result.InsertSolution(solution1);
            }
            else
            {
                Vector2 solution2 = midpt - v1;
                dist1 = FindDistanceToCurve(new PointF((float)solution2.X, (float)solution2.Y), track, out positRecue);
                //if ((solution2.X - segArea.endPoint.X) * v1.X + (solution2.Y - segArea.endPoint.Y) * v1.Y > 0 &&
                //    (solution2.X - segArea.startPoint.X) * v1.X + (solution2.Y - segArea.startPoint.Y) * v1.Y < 0)
                if (dist1 <= 0.1)
                {
                    result = new PointF((float)solution2.X, (float)solution2.Y);    //result.InsertSolution(solution2);
                }
            }

            return result;
        }

#endif