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
	class LazItemReader_GPSTIME11_v2 : LazItemReader
	{
		const int LASZIP_GPSTIME_MULTI=500;
		const int LASZIP_GPSTIME_MULTI_MINUS=-10;
		const int LASZIP_GPSTIME_MULTI_UNCHANGED=(LASZIP_GPSTIME_MULTI-LASZIP_GPSTIME_MULTI_MINUS+1);
		const int LASZIP_GPSTIME_MULTI_CODE_FULL=(LASZIP_GPSTIME_MULTI-LASZIP_GPSTIME_MULTI_MINUS+2);

		const int LASZIP_GPSTIME_MULTI_TOTAL=(LASZIP_GPSTIME_MULTI-LASZIP_GPSTIME_MULTI_MINUS+6);

		public LazItemReader_GPSTIME11_v2(ArithmeticDecoder dec)
		{
			// set decoder
			Debug.Assert(dec!=null);
			this.dec=dec;

			// create entropy models and integer compressors
			m_gpstime_multi=dec.createSymbolModel(LASZIP_GPSTIME_MULTI_TOTAL);
			m_gpstime_0diff=dec.createSymbolModel(6);
			ic_gpstime=new IntegerCompressor(dec, 32, 9); // 32 bits, 9 contexts
		}

		public override bool Init(LasPoint item)
		{
			// init state
			last=0; next=0;
			last_gpstime_diff[0]=0;
			last_gpstime_diff[1]=0;
			last_gpstime_diff[2]=0;
			last_gpstime_diff[3]=0;
			multi_extreme_counter[0]=0;
			multi_extreme_counter[1]=0;
			multi_extreme_counter[2]=0;
			multi_extreme_counter[3]=0;

			// init models and integer compressors
			dec.initSymbolModel(m_gpstime_multi);
			dec.initSymbolModel(m_gpstime_0diff);
			ic_gpstime.initDecompressor();

			// init last item
			last_gpstime[0].f64=item.gps_time;
			last_gpstime[1].u64=0;
			last_gpstime[2].u64=0;
			last_gpstime[3].u64=0;
			return true;
		}

		public override void Read(LasPoint item)
		{
			if(last_gpstime_diff[last]==0) // if the last integer difference was zero
			{
				int multi=(int)dec.decodeSymbol(m_gpstime_0diff);
				if(multi==1) // the difference can be represented with 32 bits
				{
					last_gpstime_diff[last]=ic_gpstime.decompress(0, 0);
					last_gpstime[last].i64+=last_gpstime_diff[last];
					multi_extreme_counter[last]=0;
				}
				else if(multi==2) // the difference is huge
				{
					next=(next+1)&3;
					last_gpstime[next].u64=(ulong)ic_gpstime.decompress((int)(last_gpstime[last].u64>>32), 8);
					last_gpstime[next].u64=last_gpstime[next].u64<<32;
					last_gpstime[next].u64|=dec.readInt();
					last=next;
					last_gpstime_diff[last]=0;
					multi_extreme_counter[last]=0;
				}
				else if(multi>2) // we switch to another sequence
				{
					last=(uint)(last+multi-2)&3;
					Read(item);
				}
			}
			else
			{
				int multi=(int)dec.decodeSymbol(m_gpstime_multi);
				if(multi==1)
				{
					last_gpstime[last].i64+=ic_gpstime.decompress(last_gpstime_diff[last], 1); ;
					multi_extreme_counter[last]=0;
				}
				else if(multi<LASZIP_GPSTIME_MULTI_UNCHANGED)
				{
					int gpstime_diff;
					if(multi==0)
					{
						gpstime_diff=ic_gpstime.decompress(0, 7);
						multi_extreme_counter[last]++;
						if(multi_extreme_counter[last]>3)
						{
							last_gpstime_diff[last]=gpstime_diff;
							multi_extreme_counter[last]=0;
						}
					}
					else if(multi<LASZIP_GPSTIME_MULTI)
					{
						if(multi<10)
							gpstime_diff=ic_gpstime.decompress(multi*last_gpstime_diff[last], 2);
						else
							gpstime_diff=ic_gpstime.decompress(multi*last_gpstime_diff[last], 3);
					}
					else if(multi==LASZIP_GPSTIME_MULTI)
					{
						gpstime_diff=ic_gpstime.decompress(LASZIP_GPSTIME_MULTI*last_gpstime_diff[last], 4);
						multi_extreme_counter[last]++;
						if(multi_extreme_counter[last]>3)
						{
							last_gpstime_diff[last]=gpstime_diff;
							multi_extreme_counter[last]=0;
						}
					}
					else
					{
						multi=LASZIP_GPSTIME_MULTI-multi;
						if(multi>LASZIP_GPSTIME_MULTI_MINUS)
						{
							gpstime_diff=ic_gpstime.decompress(multi*last_gpstime_diff[last], 5);
						}
						else
						{
							gpstime_diff=ic_gpstime.decompress(LASZIP_GPSTIME_MULTI_MINUS*last_gpstime_diff[last], 6);
							multi_extreme_counter[last]++;
							if(multi_extreme_counter[last]>3)
							{
								last_gpstime_diff[last]=gpstime_diff;
								multi_extreme_counter[last]=0;
							}
						}
					}
					last_gpstime[last].i64+=gpstime_diff;
				}
				else if(multi==LASZIP_GPSTIME_MULTI_CODE_FULL)
				{
					next=(next+1)&3;
					last_gpstime[next].u64=(ulong)ic_gpstime.decompress((int)(last_gpstime[last].u64>>32), 8);
					last_gpstime[next].u64=last_gpstime[next].u64<<32;
					last_gpstime[next].u64|=dec.readInt();
					last=next;
					last_gpstime_diff[last]=0;
					multi_extreme_counter[last]=0;
				}
				else if(multi>=LASZIP_GPSTIME_MULTI_CODE_FULL)
				{
					last=(uint)(last+multi-LASZIP_GPSTIME_MULTI_CODE_FULL)&3;
					Read(item);
				}
			}
			item.gps_time=last_gpstime[last].f64;
		}

		ArithmeticDecoder dec;
		uint last, next;
		U64I64F64[] last_gpstime=new U64I64F64[4];
		int[] last_gpstime_diff=new int[4];
		int[] multi_extreme_counter=new int[4];

		ArithmeticModel m_gpstime_multi;
		ArithmeticModel m_gpstime_0diff;
		IntegerCompressor ic_gpstime;
	}
}
