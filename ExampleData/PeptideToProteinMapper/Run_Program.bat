..\..\PeptideToProteinMapper\bin\Debug\PeptideToProteinMapper.exe ..\QC_Mam_19_01_a_12Oct20_Pippin_Rep-20-08-08_msgfplus.tsv /O:. /R:..\M_musculus_Uniprot_SPROT_2013_09_2013-09-18_Tryp_Pig_Bov.fasta /A
..\..\PeptideToProteinMapper\bin\Debug\PeptideToProteinMapper.exe ..\ProteinAndPeptide.txt /O:. /R:..\M_musculus_Uniprot_SPROT_2013_09_2013-09-18_Tryp_Pig_Bov.fasta /A
..\..\PeptideToProteinMapper\bin\Debug\PeptideToProteinMapper.exe ..\PeptideOnly.txt       /O:. /R:..\M_musculus_Uniprot_SPROT_2013_09_2013-09-18_Tryp_Pig_Bov.fasta /A

if not exist QC_Shew_Mode1 mkdir QC_Shew_Mode1
if not exist QC_Shew_Mode6 mkdir QC_Shew_Mode6

..\..\PeptideToProteinMapper\bin\Debug\PeptideToProteinMapper.exe QC_Shew_excerpt.txt /R:Shewanella_2006-07-11.fasta /A
..\..\PeptideToProteinMapper\bin\Debug\PeptideToProteinMapper.exe QC_Shew_excerpt.txt /R:Shewanella_2006-07-11.fasta /A /F:1 /O:QC_Shew_Mode1
..\..\PeptideToProteinMapper\bin\Debug\PeptideToProteinMapper.exe QC_Shew_excerpt.txt /R:Shewanella_2006-07-11.fasta /A /F:6 /O:QC_Shew_Mode6

pause
