"""Authored vector character study. Coordinates are drawn from visual reference.

No raster tracing, image embedding or image sampling. The compact master keeps
Bezier contours; the 1600-element version splits visible ink for process playback.
"""
from pathlib import Path
import math, re, json, html

ROOT = Path(__file__).resolve().parent
INK='#222c40'; HAIR='#eef0ef'; HS='#aeb8cb'; HL='#fffcf2'
SKIN='#f5c8a5'; SS='#da9b85'; IVORY='#f5ebd1'; CS='#b7b8ae'
NAVY='#334561'; NS='#222e47'; JADE='#438e87'; JS='#285c66'; GOLD='#cba45f'
parts=[]; paths=[]

def part(name):
    parts.append(name)

def p(name,d,fill='none',stroke=None,width=3,stage=3,opacity=1):
    assert stroke is None or isinstance(stroke,str),(name,stroke)
    paths.append(dict(id=name,slot=parts[-1],d=d,fill=fill,stroke=stroke,width=width,stage=stage,opacity=opacity))

def shape(name,d,fill,stage=2,width=6):
    p(name,d,fill,INK,width,stage)

def line(name,d,color=INK,width=3,stage=5,opacity=1):
    p(name,d,'none',color,width,stage,opacity)

def ellipse(name,cx,cy,rx,ry,fill,stage=4,stroke=None,width=2):
    k=.55228475
    d=f'M {cx-rx} {cy} C {cx-rx} {cy-ry*k} {cx-rx*k} {cy-ry} {cx} {cy-ry} C {cx+rx*k} {cy-ry} {cx+rx} {cy-ry*k} {cx+rx} {cy} C {cx+rx} {cy+ry*k} {cx+rx*k} {cy+ry} {cx} {cy+ry} C {cx-rx*k} {cy+ry} {cx-rx} {cy+ry*k} {cx-rx} {cy} Z'
    p(name,d,fill,stroke,width,stage)

# Behind the body: long ribbon, tied hair and neck hair.
part('Hair_Back')
shape('ribbon-far','M 379 273 C 355 300 333 332 324 356 L 309 344 C 333 302 349 279 370 263 Z',JS)
p('ribbon-far-light','M 368 280 Q 342 318 322 347 L 318 343 Q 339 302 365 278 Z','#589e98')
shape('ribbon-near','M 369 267 C 343 301 296 313 264 326 L 278 338 C 269 359 282 377 303 385 C 316 348 342 309 377 283 Z',JADE)
p('ribbon-near-shadow','M 275 339 Q 308 327 344 302 Q 318 332 303 378 Q 281 366 275 339 Z',JS)
p('ribbon-fold-light','M 272 328 Q 330 309 357 282 Q 337 315 286 339 Z','#65a59b')
line('ribbon-fold','M 279 339 Q 324 323 354 294',JS,3.4)
shape('bun','M 376 283 C 359 263 370 238 393 222 C 370 221 357 231 347 241 C 347 204 376 176 405 162 C 397 144 395 128 404 114 C 421 140 456 112 491 117 L 482 101 C 518 112 543 139 550 169 C 570 181 574 196 560 216 C 550 244 526 259 501 267 C 464 282 416 287 393 290 L 401 273 C 388 278 383 285 376 291 Z',HAIR)
p('bun-cool-shadow','M 352 229 C 382 201 426 209 452 219 C 413 216 399 248 385 277 L 377 284 C 366 265 374 241 397 222 Q 376 217 352 229 Z',HS)
p('bun-upper-shadow','M 406 127 Q 439 146 480 155 Q 455 130 438 127 Q 472 113 499 129 Q 491 116 487 110 C 517 125 537 146 542 173 L 527 186 C 497 150 450 162 422 153 Z','#c3c8d4')
p('bun-white-ridge','M 405 120 C 419 142 465 137 484 171 C 451 149 424 165 402 169 C 418 158 428 155 439 154 Q 404 145 405 120 Z',HL)
p('bun-sweep','M 358 218 C 383 183 425 171 462 194 C 431 186 413 191 401 201 Q 428 198 446 213 C 408 203 379 216 358 218 Z',HL)
p('bun-tip-light','M 496 114 Q 531 141 543 167 Q 548 194 523 206 Q 520 153 496 114 Z',HL)
line('bun-flow-1','M 398 166 C 430 157 461 176 484 203','#7f8da6',2.5)
line('bun-flow-2','M 384 268 C 390 241 414 224 438 221','#8390a9',2.5)
line('bun-flow-3','M 449 133 Q 489 133 510 173','#8c97ab',2.5)
ellipse('ribbon-knot',367,273,11,12,GOLD,4,INK,3)
ellipse('ribbon-knot-light',364,269,4,4,'#f4dda0')
shape('neck-hair','M 428 438 C 400 450 393 480 398 508 L 415 496 Q 411 515 437 519 L 431 528 Q 451 536 465 523 L 486 533 L 496 503 Z',HAIR)
p('neck-hair-shadow','M 433 459 Q 407 494 430 511 L 443 505 L 438 519 Q 458 520 471 508 L 480 519 L 484 497 Z',HS)
line('neck-hair-tuft','M 420 490 Q 426 501 442 500 M 440 517 L 457 508','#77849c',2.8)

part('Boot_Back')
shape('back-boot','M 577 795 C 590 791 609 795 621 799 L 622 820 C 638 824 662 833 673 846 Q 678 856 666 861 Q 620 880 575 858 C 568 846 574 829 576 818 Z',NAVY)
p('back-boot-shadow','M 578 807 L 617 811 L 615 826 Q 594 831 592 853 L 575 849 Z',NS)
p('back-boot-toe','M 622 828 Q 650 834 665 847 Q 629 862 596 850 Q 604 831 622 828 Z','#45566f')
line('back-sole','M 575 850 Q 620 870 670 851',GOLD,5.5,4)
line('back-cuff','M 578 810 Q 599 818 621 810',GOLD,7,4)
line('back-boot-fold','M 582 826 Q 600 823 611 826','#5b6b7e',3)

part('Boot_Front')
shape('front-boot','M 436 810 Q 461 806 492 816 L 488 838 C 489 855 496 873 491 889 Q 460 907 418 889 C 414 880 426 855 430 843 Z',NAVY)
p('front-boot-shadow','M 439 824 Q 455 831 481 826 L 475 850 Q 457 850 443 873 L 423 879 Q 433 846 439 824 Z',NS)
p('front-boot-toe','M 445 869 Q 459 855 482 859 L 486 882 Q 456 893 426 884 Z','#435673')
line('front-sole','M 419 883 Q 452 900 491 884',GOLD,5.5,4)
line('front-boot-cuff','M 435 825 Q 459 838 489 827',GOLD,7,4)
line('front-boot-crease','M 438 849 Q 456 842 480 848','#5a6a80',3)

part('Leg_Back')
shape('back-trouser','M 548 681 Q 580 667 614 687 C 622 723 640 750 645 776 L 637 793 Q 610 819 571 806 L 558 790 Q 572 761 549 741 L 536 715 Z',NAVY)
p('back-trouser-shadow','M 565 695 L 592 712 C 602 746 621 754 631 778 L 621 791 Q 600 807 578 799 L 570 791 Q 587 762 569 741 Z',NS)
p('back-trouser-light','M 579 694 Q 603 701 609 721 L 624 756 Q 600 744 591 724 Z','#4e5f7a')
p('back-knee-plane','M 599 756 L 620 774 L 599 792 Q 583 786 584 777 Z','#2b3852')
line('back-knee-fold','M 624 766 Q 615 785 594 792',NS,3)
line('back-crotch-fold','M 557 717 Q 573 728 577 749',NS,3.2)

part('Leg_Front')
shape('front-trouser','M 514 676 Q 544 674 571 695 C 562 729 550 754 539 781 L 532 805 Q 494 836 440 822 L 427 802 Q 444 768 460 746 C 483 723 496 698 514 676 Z',NAVY)
p('front-trouser-shadow','M 549 692 L 562 704 Q 543 754 527 795 Q 488 824 447 814 L 436 802 L 457 782 Q 469 794 480 803 C 511 774 523 731 549 692 Z',NS)
p('front-thigh-light','M 510 697 L 531 698 Q 511 740 486 771 L 453 798 C 464 759 489 732 510 697 Z','#4c5d79')
p('front-knee-plane','M 503 760 L 486 801 L 465 812 L 481 784 Z','#40516d')
line('front-thigh-fold','M 536 711 Q 520 741 500 757','#1e2d45',3)
line('front-trouser-crease','M 443 799 L 457 807 M 476 817 Q 501 811 515 802','#607089',2.5)

part('Arm_Back')
shape('back-hand','M 680 673 C 693 676 704 691 707 704 Q 711 719 704 722 Q 697 720 695 709 L 692 700 Q 690 698 688 702 L 691 716 Q 685 726 679 716 L 674 700 L 664 691 Z',SKIN,width=5)
p('back-hand-shadow','M 686 683 Q 695 689 699 702 L 702 717 Q 698 716 697 706 L 691 696 Q 685 693 681 698 L 675 692 Z',SS)
line('back-hand-thumb','M 687 700 L 682 693','#af746b',2.2)

part('Torso')
shape('neck','M 524 509 L 577 508 L 585 554 L 564 583 L 526 555 Z',SKIN)
p('neck-shadow','M 527 518 Q 552 530 577 521 L 579 541 L 555 554 L 529 542 Z',SS)
shape('tunic-body','M 479 541 C 506 529 523 535 543 546 L 571 559 L 584 537 C 604 537 619 544 625 568 L 625 638 Q 624 659 607 665 L 488 660 C 476 630 463 579 479 541 Z',NAVY)
p('torso-shadow','M 571 560 L 585 547 L 600 566 Q 609 603 613 635 Q 583 654 537 644 L 554 623 Z',NS)
p('torso-chest-plane','M 521 550 L 554 573 L 544 610 Q 528 603 516 581 Z','#455670')
line('inner-collar-gold','M 520 540 L 559 567 L 574 542',GOLD,6,4)
shape('ivory-left-lapel','M 480 541 Q 499 537 510 548 C 519 563 530 596 535 630 L 542 647 L 505 643 Q 488 623 484 602 Z',IVORY,width=4.5)
p('left-lapel-shadow','M 490 551 Q 507 574 510 605 Q 511 630 528 641 L 508 639 Q 496 622 493 603 Z',CS)
p('left-lapel-light','M 481 544 Q 491 543 499 551 Q 501 577 505 591 Q 491 574 481 568 Z',HL)
shape('cross-lapel','M 587 540 L 601 549 C 594 580 575 611 552 636 L 531 635 Q 564 606 582 562 Z',IVORY,width=3.5)
p('cross-lapel-shadow','M 594 548 L 599 551 C 592 580 573 610 551 633 L 542 632 Q 576 599 594 548 Z','#d0cbbb')
line('chest-fold','M 587 611 Q 580 627 567 633 M 598 627 L 607 620','#51617a',2.5)

part('Sleeve_Back')
shape('back-sleeve','M 605 546 Q 622 541 629 558 L 637 582 L 646 598 L 635 609 Q 639 629 657 644 L 698 670 L 683 686 C 659 686 636 674 617 665 Q 619 642 616 617 L 611 591 Z',IVORY)
p('back-sleeve-shadow','M 626 564 L 633 586 L 639 598 L 630 607 L 638 632 L 627 648 L 622 625 L 622 598 Z',CS)
p('back-sleeve-fold','M 641 636 L 658 646 L 650 660 L 682 678 Q 652 677 632 664 Z','#d5cebd')
p('back-sleeve-light','M 646 642 L 657 649 L 680 665 L 655 661 Z',HL)
line('back-sleeve-trim','M 637 584 Q 637 597 625 602 M 627 664 Q 658 685 688 675',GOLD,5,4)
line('back-sleeve-crease','M 635 610 L 635 621 L 645 627 M 653 647 L 649 656','#9b9da1',2.5)

part('Coat')
shape('coat-back-panel','M 598 663 L 617 666 Q 638 704 676 735 L 647 765 L 627 777 Q 608 732 598 696 Z',IVORY)
p('coat-back-shadow','M 608 677 L 620 689 L 640 722 L 666 740 L 647 758 Q 628 723 608 707 Z',CS)
p('coat-back-light','M 617 680 Q 640 716 667 736 L 650 747 Q 637 714 617 680 Z',HL)
line('coat-back-gold','M 610 694 Q 632 729 660 743 L 642 759',GOLD,5.5,4)
shape('coat-front-panel','M 488 655 L 551 670 Q 545 692 532 711 Q 499 731 478 778 C 456 768 439 752 430 736 Q 455 690 488 655 Z',IVORY)
p('coat-front-shadow','M 488 663 L 512 670 Q 482 692 459 725 L 438 737 Q 457 703 488 663 Z',CS)
p('coat-front-fold','M 513 680 L 540 676 L 528 704 Q 499 725 478 766 L 466 751 Q 480 714 513 680 Z','#e0d7c2')
p('coat-front-light','M 492 676 L 513 676 Q 484 704 463 735 L 442 737 Q 462 699 492 676 Z',HL)
line('coat-front-gold','M 442 742 Q 454 758 471 766 Q 488 735 507 722',GOLD,5.5,4)
shape('navy-sash','M 552 674 L 579 673 Q 591 714 628 737 L 584 750 L 561 710 Z',NS,width=4)
p('sash-light','M 561 681 L 575 680 Q 588 714 615 734 L 587 741 Q 575 718 561 697 Z',NAVY)
line('sash-fold','M 569 695 Q 582 710 601 720','#52627c',2.7)

part('Sleeve_Front')
shape('front-shoulder','M 481 544 C 459 530 441 537 426 554 L 405 584 L 415 595 Q 436 591 456 600 L 478 620 Q 491 600 487 578 Z',IVORY)
p('shoulder-shadow','M 478 551 Q 478 578 486 594 L 478 610 Q 467 596 451 591 Q 467 573 466 555 Z',CS)
p('shoulder-light','M 440 548 Q 457 537 471 549 Q 462 561 459 576 Q 442 566 424 582 Q 431 558 440 548 Z',HL)
line('shoulder-gold','M 411 581 Q 443 579 477 605',GOLD,5,4)
shape('front-sleeve','M 414 596 Q 440 588 463 609 L 450 628 Q 452 644 463 670 L 467 680 Q 454 697 438 704 L 408 718 Q 381 726 359 719 C 347 708 333 690 327 677 Q 344 660 369 642 Q 390 624 398 611 Z',IVORY)
p('front-sleeve-shadow','M 446 610 L 452 616 L 443 630 Q 451 656 459 676 L 440 689 Q 420 684 399 692 L 385 715 L 365 715 Q 358 696 363 683 L 397 663 Q 427 643 446 610 Z',CS)
p('front-sleeve-fold','M 394 628 Q 411 616 436 616 Q 416 633 405 650 L 366 666 Z','#d5d0c0')
p('front-sleeve-light','M 397 617 Q 413 602 437 609 Q 415 616 405 629 L 386 651 L 350 674 L 338 674 Q 365 648 387 635 Z',HL)
p('sleeve-rim-light','M 337 684 L 357 708 L 359 681 Q 386 668 410 662 L 405 667 Q 377 676 362 689 L 361 716 Q 349 709 337 684 Z',HL)
line('front-sleeve-gold','M 332 674 Q 349 681 366 674 Q 397 654 415 654 Q 435 657 460 674',GOLD,6,4)
line('front-sleeve-crease','M 403 607 Q 413 609 419 615 M 435 623 Q 420 641 414 646 M 362 689 L 362 706','#95989d',2.8)
shape('front-cuff','M 394 677 Q 416 661 439 681 L 445 707 L 432 724 L 388 714 Z',NAVY,width=4.5)
p('front-cuff-shadow','M 417 675 Q 431 675 437 685 L 441 704 L 429 711 L 414 688 Z',NS)
line('front-cuff-gold','M 390 707 Q 413 687 439 703',GOLD,6,4)

part('Belt')
shape('belt','M 493 640 Q 550 650 610 638 L 619 654 L 608 674 Q 550 685 488 664 Z',JADE,width=5)
p('belt-shadow','M 492 654 Q 548 667 608 651 L 611 665 Q 550 680 491 663 Z',JS)
p('belt-light','M 499 645 Q 552 655 596 645 L 596 650 Q 543 662 496 652 Z','#80b6a1')
shape('belt-tail','M 615 661 Q 644 685 676 708 L 669 723 Q 639 691 610 674 Z',JADE,width=4)
p('belt-tail-shadow','M 620 674 Q 644 691 670 714 L 669 719 Q 641 692 620 680 Z',JS)
ellipse('buckle',614,654,17,17,JS,4,INK,4)
ellipse('buckle-rim',612,652,12,12,'#7ebcaa',4)
ellipse('buckle-inset',615,654,7,8,JADE,4)
line('buckle-glint','M 605 653 Q 604 644 613 644','#d1ead5',3.5,4)

part('Arm_Front')
shape('front-hand','M 405 701 C 420 695 431 701 437 712 Q 451 731 441 736 Q 432 738 428 724 C 428 740 427 748 417 750 C 400 749 388 739 389 725 Q 389 708 405 701 Z',SKIN,width=5)
p('front-hand-shadow','M 400 707 Q 390 722 397 737 Q 404 745 416 748 Q 398 751 391 735 Q 387 717 400 707 Z',SS)
p('front-hand-light','M 406 707 Q 419 703 427 712 Q 413 708 405 718 Z','#ffe1bb')
line('thumb-joint','M 428 724 Q 426 716 422 714','#b87565',2.4)
line('front-finger-fold','M 396 734 Q 396 739 400 741','#c18672',2)

# Full skull and face are explicitly vector geometry, separate from hair/eyes.
part('Head')
shape('head','M 455 332 C 483 299 545 296 591 311 C 638 324 668 357 668 403 L 660 465 C 655 503 622 529 580 534 C 540 540 493 531 467 512 L 447 489 C 425 488 414 477 414 462 Q 412 443 433 438 L 441 399 Z',SKIN,width=5.5)
p('face-side-shadow','M 627 351 C 651 373 662 403 661 429 L 654 466 Q 650 497 623 515 Q 646 484 643 447 Q 642 404 615 378 Z',SS)
p('face-upper-shadow','M 451 391 Q 501 354 563 366 Q 619 371 642 413 L 633 432 Q 611 410 598 410 Q 560 426 528 454 L 483 469 L 447 467 Z','#eeb38d')
p('jaw-warmth','M 462 501 Q 509 533 571 528 Q 607 528 631 511 Q 609 531 578 535 Q 510 539 474 518 Z','#e6aa8a')
p('face-light','M 522 461 Q 563 433 600 423 Q 621 432 625 451 Q 603 474 597 498 Q 565 524 525 510 Z','#fbd1ab',stage=3)
ellipse('ear-inner',437,468,12,16,SS,3)
p('ear-light','M 427 457 Q 441 447 450 457 L 441 463 Q 430 459 431 475 Q 424 467 427 457 Z','#ffddba')
line('ear-fold','M 429 471 Q 430 459 440 461 Q 446 464 439 471','#bb806e',2.5)

part('Eyes')
shape('far-eye-white','M 610 454 C 626 438 640 433 653 433 C 649 451 642 474 629 485 C 618 490 610 478 610 454 Z','#fcf6e6',4,width=3.6)
p('far-eye-upper-shade','M 612 455 Q 634 439 650 437 L 644 447 Q 628 446 612 463 Z','#b6c0bc',stage=4)
shape('far-iris','M 624 452 Q 635 442 641 448 C 644 462 636 478 628 482 C 618 482 617 467 624 452 Z',JADE,4,width=2)
p('far-iris-dark','M 623 454 Q 633 445 641 449 L 638 462 Q 628 460 621 469 Z',JS,stage=4)
p('far-iris-glow','M 623 471 Q 630 472 635 463 Q 635 476 627 479 Z','#85c8b0',stage=4)
ellipse('far-pupil',631,458,4,9,'#182c37',4)
ellipse('far-eye-glint',635,451,3,4,'#fffdef',4)
line('far-lid','M 609 453 Q 627 437 653 431',INK,7.2,4)
line('far-lash','M 650 434 L 657 426',INK,4,4)
line('far-brow','M 616 435 Q 631 424 647 420','#654f4a',3.5,4)
shape('near-eye-white','M 489 479 Q 524 466 559 477 Q 551 505 531 510 C 511 514 497 500 489 479 Z','#fffae9',4,width=3.8)
p('near-eye-upper-shade','M 493 480 Q 525 470 555 479 L 552 486 Q 519 480 499 489 Z','#bdc5bd',stage=4)
shape('near-iris','M 519 477 Q 535 472 547 480 C 551 493 542 506 531 508 Q 514 510 516 489 Z',JADE,4,width=2)
p('near-iris-dark','M 519 479 Q 533 473 546 481 L 546 489 Q 533 485 516 493 Z',JS,stage=4)
p('near-iris-glow','M 521 497 Q 534 503 544 491 Q 541 505 531 505 Q 524 506 521 497 Z','#90cbb3',stage=4)
ellipse('near-pupil',532,486,5,11,'#182c38',4)
ellipse('near-glint',538,480,4,4.5,'#fffef3',4)
ellipse('near-lower-glint',523,498,2,2,'#b9dfbf',4)
line('near-lid','M 486 478 C 508 468 538 469 559 478',INK,8,4)
line('near-lash','M 492 480 L 483 470',INK,4,4)
line('near-brow','M 493 462 Q 521 450 548 462','#72584c',3.5,4)

part('Mouth')
line('nose','M 601 493 L 603 498 L 600 499','#cc9478',2,4)
line('smile','M 579 516 Q 592 522 607 513','#745647',2.5,4)
line('smile-corner','M 606 513 L 609 510','#ad7c65',1.7,4)
line('near-cheek','M 496 504 L 501 506 M 504 508 L 508 509','#e8a78e',2.3,4)
line('far-cheek','M 637 487 L 640 485','#e8a78e',2,4)

part('Hair_Front')
shape('hair-mass','M 415 273 C 440 235 491 217 539 222 C 578 216 605 223 623 235 Q 627 215 613 205 C 644 211 660 231 665 247 Q 698 249 714 279 L 692 273 Q 706 288 710 302 C 732 332 750 326 762 314 C 758 340 743 352 720 356 Q 734 382 756 390 Q 738 400 717 390 Q 732 420 718 458 L 705 438 Q 701 466 680 481 L 683 453 L 663 479 L 663 445 C 661 414 645 389 632 378 Q 618 385 612 403 Q 607 449 579 471 L 575 448 L 563 479 Q 538 460 538 432 C 511 463 480 485 443 493 Q 453 477 454 463 Q 432 480 412 479 L 426 465 Q 388 486 358 472 L 377 459 Q 341 451 328 417 Q 349 427 368 418 C 335 415 337 383 348 356 L 354 336 Q 377 299 415 273 Z',HAIR)
# Large hair planes, with a deliberately limited set of clumps.
p('hair-left-underplane','M 415 281 Q 379 322 395 353 C 403 374 418 380 443 378 Q 410 397 387 386 Q 385 407 408 418 Q 382 431 350 423 L 339 421 Q 351 440 375 446 L 400 435 Q 385 454 372 467 Q 403 471 426 449 L 438 424 Q 437 400 460 373 Q 430 350 433 316 Z',HS)
p('hair-left-middle-plane','M 368 336 Q 351 384 370 405 Q 363 371 383 344 Q 377 373 392 386 L 399 379 Q 380 363 395 322 Z','#cbd0da')
p('hair-top-plane','M 418 284 Q 440 245 486 234 Q 470 252 477 271 Q 495 295 522 296 Q 482 300 458 276 Q 467 312 505 318 Q 463 329 433 302 Q 438 337 470 351 Q 432 345 417 321 Z','#c0c6d2')
p('hair-top-light','M 427 275 Q 450 242 491 232 Q 476 248 480 260 Q 489 277 507 284 Q 474 272 463 259 Q 445 261 427 275 Z',HL)
p('hair-side-light','M 397 298 Q 384 327 397 350 Q 410 369 438 373 Q 417 378 402 368 Q 375 349 380 322 Z',HL)
p('hair-left-tip-light','M 340 427 Q 370 436 397 424 Q 385 443 367 447 Q 350 439 340 427 Z',HL)
p('hair-temple-shadow','M 442 436 Q 431 462 444 484 L 426 494 L 410 479 L 423 470 Q 420 452 442 436 Z','#929fb7')
p('hair-right-shadow','M 656 266 Q 687 278 696 310 Q 709 331 739 337 Q 723 349 704 340 L 689 328 Q 697 360 731 382 Q 709 380 693 364 Q 711 400 706 435 L 694 419 Q 692 452 681 469 L 683 443 Q 678 412 663 394 L 649 381 L 635 392 Q 632 366 646 350 Q 669 324 656 266 Z',HS)
p('hair-right-light','M 665 260 Q 691 270 700 291 Q 678 279 673 282 Q 697 301 698 318 Q 680 297 664 293 Z',HL)
p('hair-side-bright','M 670 308 Q 681 321 687 341 Q 691 361 707 372 Q 690 367 678 351 L 685 379 Q 668 368 661 350 Z',HL)
p('hair-right-flip','M 714 320 Q 734 340 754 327 Q 742 344 722 341 Z',HL)
shape('crown-lock','M 478 285 C 494 301 519 294 543 282 Q 579 264 602 273 Q 615 279 619 298 C 615 321 600 338 576 347 L 548 339 C 522 331 493 328 478 285 Z',HAIR,width=3)
p('crown-lock-shadow','M 486 300 Q 515 320 550 304 Q 590 281 609 291 L 614 303 Q 580 310 559 332 Q 518 332 496 316 Z','#bcc4d4')
p('crown-lock-light','M 490 297 Q 515 304 546 287 Q 578 271 598 279 Q 573 280 550 295 Q 516 317 490 297 Z',HL)
shape('upturned-lock','M 605 284 Q 626 239 615 207 Q 646 216 654 247 C 661 268 644 287 616 296 Z',HAIR,width=4.5)
p('upturned-shadow','M 632 219 Q 658 256 624 285 L 615 291 L 629 268 Q 642 244 632 219 Z','#c1c8d6')
p('upturned-light','M 623 219 Q 639 234 637 248 Q 635 266 619 282 Q 635 246 623 219 Z',HL)
shape('center-fringe','M 604 304 C 578 305 552 321 522 346 C 495 373 476 406 452 439 L 431 473 Q 468 462 497 435 C 477 459 462 481 444 492 C 484 487 515 461 541 430 C 545 454 556 471 578 481 Q 565 460 573 440 C 594 411 611 382 616 354 C 620 331 618 315 604 304 Z',HAIR,width=4)
p('fringe-left-shadow','M 540 337 C 516 362 501 395 474 423 L 442 468 Q 476 449 495 429 Q 488 449 473 465 Q 518 432 541 397 Q 562 366 580 351 C 562 355 553 362 540 374 Q 543 353 557 334 Z','#c2c9d7')
p('fringe-front-light','M 595 313 C 561 325 537 348 519 375 L 505 404 C 530 385 543 365 564 361 Q 575 360 583 353 Q 594 340 609 335 Q 613 321 603 314 Z',HL)
p('fringe-lower-light','M 529 401 Q 502 444 467 473 Q 511 450 543 410 L 551 392 Z',HL)
p('fringe-right-shadow','M 607 339 Q 606 380 579 421 Q 557 449 570 470 Q 550 455 547 431 Q 574 399 588 368 Z',HS)
shape('right-fringe','M 618 327 Q 641 323 657 345 Q 682 378 674 427 L 662 449 Q 665 405 632 374 Q 619 380 611 400 Q 620 365 618 327 Z',HAIR,width=3.5)
p('right-fringe-shadow','M 632 340 Q 658 362 664 393 L 670 422 L 665 434 Q 658 397 632 374 Q 623 376 616 388 L 620 374 Q 623 355 632 340 Z',HS)
p('right-fringe-light','M 625 332 Q 652 337 662 369 Q 646 347 633 352 L 625 365 Z',HL)
line('hair-flow-top','M 491 233 Q 514 238 529 253 M 546 227 Q 575 237 588 256','#8895ad',2.4)
line('hair-flow-left','M 407 294 Q 397 318 409 337 M 377 373 Q 379 396 396 405','#8794ad',2.3)
line('hair-flow-fringe','M 578 317 Q 551 330 536 347 M 512 395 Q 496 421 479 438','#8b98ad',2.1)
line('hair-flow-right','M 678 287 Q 687 292 695 300 M 691 390 Q 697 410 690 429','#8996ae',2.1)

part('Hair_Accessory')
shape('hair-band','M 415 269 C 437 236 468 216 492 207 L 511 221 C 474 229 440 249 420 280 Z',JS,width=4.5)
p('band-lit-face','M 428 263 C 450 237 473 224 498 217 L 493 211 Q 454 225 428 252 Z',JADE)
line('band-gold','M 435 248 Q 458 226 489 216',GOLD,4.5,4)
shape('jade-ornament','M 504 178 C 526 174 542 191 539 211 Q 557 220 546 235 C 532 246 516 237 513 226 C 494 228 479 215 486 197 Q 491 181 504 178 Z',JADE,4,width=4.5)
p('jade-shadow','M 524 183 C 541 196 526 211 515 208 Q 527 218 516 228 Q 518 238 534 238 Q 549 235 547 225 L 534 215 Q 540 198 524 183 Z',JS,stage=4)
p('jade-lit-face','M 503 184 Q 519 178 526 191 C 511 185 499 194 498 207 Q 499 217 508 220 Q 489 218 490 204 Q 490 191 503 184 Z','#85bfaa',stage=4)
line('jade-spiral','M 521 197 C 506 190 502 210 515 212 C 528 215 533 203 527 194','#b6dec6',4,4)
ellipse('jade-pin-light',500,190,3,3,'#e9f2cf',4)

def attrs(row,fill=None,stroke=None,ident=None):
    return f'<path id="{ident or row["id"]}" d="{row["d"]}" fill="{fill if fill is not None else row["fill"]}"'+(f' stroke="{stroke}" stroke-width="{row["width"]}"' if stroke else ' stroke="none"')+(f' opacity="{row["opacity"]}"' if row['opacity']!=1 else '')+'/>'

def tokenize(d):return re.findall(r'[MLCQZ]|-?(?:\d*\.\d+|\d+)',d)

def points(d):
    tokens=tokenize(d);i=0;cur=(0.,0.);start=cur;sub=[];allsub=[]
    def pair():
        nonlocal i
        x,y=float(tokens[i]),float(tokens[i+1]);i+=2;return x,y
    while i<len(tokens):
        cmd=tokens[i];i+=1
        if cmd=='M':
            if sub:allsub.append(sub)
            cur=pair();start=cur;sub=[cur]
        elif cmd=='L':cur=pair();sub.append(cur)
        elif cmd in ['C','Q']:
            a=cur;b=pair();c=pair();e=pair() if cmd=='C' else c
            poly=[a,b,c,e] if cmd=='C' else [a,b,c]
            length=sum(math.dist(poly[k],poly[k+1]) for k in range(len(poly)-1));n=max(12,math.ceil(length/2))
            for k in range(1,n+1):
                t=k/n;u=1-t
                if cmd=='C':pt=tuple(u**3*a[j]+3*u*u*t*b[j]+3*u*t*t*c[j]+t**3*e[j] for j in [0,1])
                else:pt=tuple(u*u*a[j]+2*u*t*b[j]+t*t*c[j] for j in [0,1])
                sub.append(pt)
            cur=e
        elif cmd=='Z':sub.append(start);cur=start
        else:raise ValueError(cmd)
    if sub:allsub.append(sub)
    return allsub

def stroke_pieces(d,n):
    subs=points(d); edges=[(a,b,math.dist(a,b)) for ss in subs for a,b in zip(ss,ss[1:]) if a!=b]
    total=sum(e[2] for e in edges);target=total/n;out=[];current=[];used=0
    for a,b,l in edges:
        while l>1e-7:
            take=min(l,target-used)
            end=(a[0]+(b[0]-a[0])*take/l,a[1]+(b[1]-a[1])*take/l)
            if not current or math.dist(current[-1],a)>.01:current.extend([None,a])
            current.append(end);used+=take;a=end;l-=take
            if used>=target-1e-6:
                out.append(current);current=[];used=0
    if current:
        if len(out)==n:out[-1].extend(current)
        else:out.append(current)
    assert len(out)==n,(len(out),n)
    result=[]
    for ps in out:
        text='';move=True
        for pt in ps:
            if pt is None:move=True;continue
            text+=('M' if move else 'L')+f'{pt[0]:.2f} {pt[1]:.2f} ';move=False
        result.append(text.strip())
    return result

header='<svg xmlns="http://www.w3.org/2000/svg" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" viewBox="0 0 1024 1024" width="1024" height="1024" stroke-linecap="round" stroke-linejoin="round">'
title='<title>Ngự Thú — mẫu C, nhân vật vector</title><desc>Vector drawn from reference A using authored Bezier paths. No embedded raster. Static study, not a rigged character.</desc>'
compact=[header,title]
elements=[]; strokes=[r for r in paths if r['stroke']]
fills=sum(r['fill']!='none' for r in paths)
budget=1600-fills
lengths=[sum(math.dist(a,b) for ss in points(r['d']) for a,b in zip(ss,ss[1:])) for r in strokes]
counts=[max(1,math.floor(budget*l/sum(lengths))) for l in lengths]
while sum(counts)<budget:
    k=max(range(len(counts)),key=lambda i:budget*lengths[i]/sum(lengths)-counts[i]);counts[k]+=1
allocated={r['id']:n for r,n in zip(strokes,counts)}
for slot in parts:
    compact.append(f'<g id="{slot}" inkscape:groupmode="layer" inkscape:label="{slot}">')
    for r in [r for r in paths if r['slot']==slot]:
        compact.append(attrs(r,stroke=r['stroke']))
        if r['fill']!='none':elements.append(dict(slot=slot,id=r['id']+'-fill',stage=r['stage'],markup=attrs(r,ident=r['id']+'-fill')))
        if r['stroke']:
            for k,d in enumerate(stroke_pieces(r['d'],allocated[r['id']])):
                copy=dict(r,d=d,fill='none');ident=r['id']+f'-ink-{k:03}'
                elements.append(dict(slot=slot,id=ident,stage=4 if r['stage']==4 else 5,markup=attrs(copy,fill='none',stroke=r['stroke'],ident=ident)))
    compact.append('</g>')
compact.append('</svg>')
assert len(elements)==1600,(len(elements),fills,sum(counts),len(strokes),len(allocated))
(ROOT/'C-editable.svg').write_text('\n'.join(compact)+'\n')

def svg(selected=None,sketch='',background=''):
    chunks=[header,title,background,sketch]
    for slot in parts:
        chunks.append(f'<g id="{slot}" inkscape:groupmode="layer" inkscape:label="{slot}">')
        chunks.extend(e['markup'] for e in elements if e['slot']==slot and (selected is None or e['id'] in selected))
        chunks.append('</g>')
    return '\n'.join(chunks+['</svg>'])

(ROOT/'C.svg').write_text(svg()+'\n')
draft_paths=[
 'M 408 108 Q 538 79 564 236 M 344 236 Q 458 167 637 234 Q 748 285 739 421 Q 696 522 583 540 Q 448 557 373 449 Q 326 345 407 267',
 'M 442 389 C 427 325 465 285 538 286 C 619 276 674 325 674 405 C 674 481 628 536 554 541 C 484 541 435 488 438 433',
 'M 594 310 Q 611 415 575 530 M 441 461 Q 553 497 665 425',
 'M 471 542 Q 539 522 613 543 L 625 654 Q 560 686 484 657 Z',
 'M 476 556 Q 434 579 412 621 L 370 675 L 414 718 M 612 559 L 637 625 L 691 701',
 'M 540 674 Q 490 725 458 811 L 452 883 M 572 678 Q 593 738 599 802 L 625 848',
 'M 261 900 L 771 900 M 536 102 L 536 900',
 'M 488 481 Q 521 463 561 479 M 608 453 Q 630 437 653 433 M 581 516 Q 597 522 608 511',
 'M 423 874 Q 452 856 491 878 M 575 844 Q 619 824 673 850',
 'M 357 695 Q 359 673 412 656 L 462 676 M 432 740 L 472 773 L 530 710 M 626 769 L 674 737 L 614 666']
sketch='<g fill="none" stroke="#618da0" stroke-width="2.6" opacity="0.75">'+''.join(f'<path d="{d}"/>' for d in draft_paths)+'</g>'
stage_names={1:'Phác hình và tỷ lệ',2:'Mảng màu lớn',3:'Bóng và khối',4:'Mắt, mặt và phụ kiện',5:'Nét và hoàn thiện'}
for phase in range(1,6):
    selected={e['id'] for e in elements if e['stage']<=phase} if phase>1 else set()
    (ROOT/'stages'/f'{phase:02}.svg').write_text(svg(selected,sketch if phase==1 else '')+'\n')
(ROOT/'drawing-data.json').write_text(json.dumps({'reference':'../A/A.png','method':'authored Bezier geometry; no automatic raster tracing','drawing_elements':len(elements),'compact_paths':len(paths),'fill_paths':fills,'ink_segments':budget,'slots':parts,'stage_names':stage_names,'elements':elements,'sketch':sketch},ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'paths_authored':len(paths),'final_elements':len(elements),'fills':fills,'ink_segments':budget,'slots':len(parts)},indent=2))
