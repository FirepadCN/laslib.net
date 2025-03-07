﻿///////////////////////////////////////////////////////////////////////
/// File    : LasItemReader.cs
/// Desc    : Las item reader abstract class.
/// Author  : Li G.Q.
/// Date    : 2021/9/13/
///////////////////////////////////////////////////////////////////////

using LasLibNet.Abstract;
using LasLibNet.Model;
using LasLibNet.Utils;
using System.Diagnostics;

namespace LasLibNet.Implement
{
	class LazItemReader_GPSTIME11_v1 : LazItemReader
	{
		const int LASZIP_GPSTIME_MULTIMAX=512;

		public LazItemReader_GPSTIME11_v1(ArithmeticDecoder dec)
		{
			// set decoder
			Debug.Assert(dec!=null);
			this.dec=dec;

			// create entropy models and integer compressors
			m_gpstime_multi=dec.createSymbolModel(LASZIP_GPSTIME_MULTIMAX);
			m_gpstime_0diff=dec.createSymbolModel(3);
			ic_gpstime=new IntegerCompressor(dec, 32, 6); // 32 bits, 6 contexts
		}

		public override bool Init(LasPoint item)
		{
			// init state
			last_gpstime_diff=0;
			multi_extreme_counter=0;

			// init models and integer compressors
			dec.initSymbolModel(m_gpstime_multi);
			dec.initSymbolModel(m_gpstime_0diff);
			ic_gpstime.initDecompressor();

			// init last item
			last_gpstime.f64=item.gps_time;

			return true;
		}

		public override void Read(LasPoint item)
		{
			if(last_gpstime_diff==0) // if the last integer difference was zero
			{
				int multi=(int)dec.decodeSymbol(m_gpstime_0diff);
				if(multi==1) // the difference can be represented with 32 bits
				{
					last_gpstime_diff=ic_gpstime.decompress(0, 0);
					last_gpstime.i64+=last_gpstime_diff;
				}
				else if(multi==2) // the difference is huge
				{
					last_gpstime.u64=dec.readInt64();
				}
			}
			else
			{
				int multi=(int)dec.decodeSymbol(m_gpstime_multi);

				if(multi<LASZIP_GPSTIME_MULTIMAX-2)
				{
					int gpstime_diff;
					if(multi==1)
					{
						gpstime_diff=ic_gpstime.decompress(last_gpstime_diff, 1);
						last_gpstime_diff=gpstime_diff;
						multi_extreme_counter=0;
					}
					else if(multi==0)
					{
						gpstime_diff=ic_gpstime.decompress(last_gpstime_diff/4, 2);
						multi_extreme_counter++;
						if(multi_extreme_counter>3)
						{
							last_gpstime_diff=gpstime_diff;
							multi_extreme_counter=0;
						}
					}
					else if(multi<10)
					{
						gpstime_diff=ic_gpstime.decompress(multi*last_gpstime_diff, 3);
					}
					else if(multi<50)
					{
						gpstime_diff=ic_gpstime.decompress(multi*last_gpstime_diff, 4);
					}
					else
					{
						gpstime_diff=ic_gpstime.decompress(multi*last_gpstime_diff, 5);
						if(multi==LASZIP_GPSTIME_MULTIMAX-3)
						{
							multi_extreme_counter++;
							if(multi_extreme_counter>3)
							{
								last_gpstime_diff=gpstime_diff;
								multi_extreme_counter=0;
							}
						}
					}
					last_gpstime.i64+=gpstime_diff;
				}
				else if(multi<LASZIP_GPSTIME_MULTIMAX-1)
				{
					last_gpstime.u64=dec.readInt64();
				}
			}

			item.gps_time=last_gpstime.f64;
		}

		ArithmeticDecoder dec;
		U64I64F64 last_gpstime;

		ArithmeticModel m_gpstime_multi;
		ArithmeticModel m_gpstime_0diff;
		IntegerCompressor ic_gpstime;
		int multi_extreme_counter;
		int last_gpstime_diff;
	}
}
